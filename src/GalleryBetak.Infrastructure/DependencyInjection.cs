using GalleryBetak.Application.Interfaces;
using GalleryBetak.Infrastructure.Data;
using GalleryBetak.Infrastructure.Identity;
using GalleryBetak.Infrastructure.Repositories;
using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using GalleryBetak.Infrastructure.Settings;
using GalleryBetak.Infrastructure.Services;

namespace GalleryBetak.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    private static string ResolveHostedSqlitePath(IHostEnvironment environment, string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            var rootedDirectory = Path.GetDirectoryName(configuredPath);
            if (!string.IsNullOrWhiteSpace(rootedDirectory))
            {
                Directory.CreateDirectory(rootedDirectory);
            }

            return configuredPath;
        }

        var fileName = Path.GetFileName(configuredPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "gallerybetak.db";
        }

        var candidates = new List<string>
        {
            Path.Combine(environment.ContentRootPath, "App_Data"),
            Path.Combine(Path.GetTempPath(), "GalleryBetak")
        };

        var homePath = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrWhiteSpace(homePath))
        {
            candidates.Insert(0, Path.Combine(homePath, "App_Data"));
        }

        foreach (var candidate in candidates)
        {
            try
            {
                Directory.CreateDirectory(candidate);
                var probeFile = Path.Combine(candidate, ".probe");
                File.WriteAllText(probeFile, "ok");
                File.Delete(probeFile);
                return Path.Combine(candidate, fileName);
            }
            catch
            {
                // Try next writable location.
            }
        }

        return Path.Combine(Path.GetTempPath(), fileName);
    }

    /// <summary>
    /// Registers all Infrastructure layer services: DbContext, Identity, Redis, repositories, UoW.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // ── Database ──────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var useInMemoryDatabase = configuration.GetValue<bool>("Database:UseInMemoryDatabase");
        var enableHostedSqliteFallback = configuration.GetValue("Database:EnableHostedSqliteFallback", true);
        var hostedSqlitePath = configuration.GetValue<string>("Database:HostedSqlitePath") ?? "App_Data/gallerybetak.db";

        var looksLikeLocalSqlConnection =
            !string.IsNullOrWhiteSpace(connectionString) &&
            (connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase) ||
             connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
             connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase));

        var shouldUseHostedSqliteFallback =
            !useInMemoryDatabase &&
            !environment.IsDevelopment() &&
            enableHostedSqliteFallback &&
            looksLikeLocalSqlConnection;

        services.AddDbContext<AppDbContext>(options =>
        {
            if (useInMemoryDatabase)
            {
                options.UseInMemoryDatabase("GalleryBetakDb");
                return;
            }

            if (shouldUseHostedSqliteFallback)
            {
                var absoluteSqlitePath = ResolveHostedSqlitePath(environment, hostedSqlitePath);

                options.UseSqlite($"Data Source={absoluteSqlitePath}");
                return;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Database:UseInMemoryDatabase is false but ConnectionStrings:DefaultConnection is missing.");
            }

            options.UseSqlServer(connectionString);
        });

        // ── ASP.NET Identity ──────────────────────────────────────────
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password policy
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredUniqueChars = 4;

                // Lockout policy
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

                // Sign-in
                options.SignIn.RequireConfirmedEmail = false; // Enable when SMTP is configured
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // ── Redis ─────────────────────────────────────────────────────
        var redisConnectionString = configuration.GetValue<string>("RedisSettings:ConnectionString");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConnectionString));

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = configuration.GetValue<string>("RedisSettings:InstanceName") ?? "GalleryBetak_";
            });
        }
        else
        {
            // Fallback to in-memory cache when Redis is not configured
            services.AddDistributedMemoryCache();
        }

        // ── Repositories & Unit of Work ───────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        // ── Services ──────────────────────────────────────────────────
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, Services.ProductService>();
        services.AddScoped<ProductImportHtmlParser>();
        services.AddScoped<IProductImportService, Services.ProductImportService>();
        services.AddScoped<IPhotoService, Services.PhotoService>();
        services.AddScoped<ICategoryService, Services.CategoryService>();
        services.AddScoped<ICartService, Services.CartService>();
        services.AddScoped<IWishlistService, Services.WishlistService>();
        services.AddScoped<IOrderService, Services.OrderService>();
        services.AddScoped<IPaymentService, Services.PaymentService>();
        services.AddScoped<IAdminDashboardService, Services.AdminDashboardService>();
        
        // Paymob Integrations
        services.Configure<GalleryBetak.Infrastructure.Settings.PaymobSettings>(
            configuration.GetSection(GalleryBetak.Infrastructure.Settings.PaymobSettings.SectionName));

        services.Configure<ProductImporterSettings>(
            configuration.GetSection(ProductImporterSettings.SectionName));
            
        services.AddHttpClient("Paymob", client =>
        {
            client.BaseAddress = new Uri("https://accept.paymob.com/api/");
        });

        services.AddHttpClient("ProductImporter", (sp, client) =>
        {
            var importerSettings = sp.GetRequiredService<IOptions<ProductImporterSettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(3, importerSettings.RequestTimeoutSeconds));
            if (!string.IsNullOrWhiteSpace(importerSettings.UserAgent))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(importerSettings.UserAgent);
            }
        });

        return services;
    }
}

