using ElMasria.Application.Interfaces;
using ElMasria.Infrastructure.Data;
using ElMasria.Infrastructure.Identity;
using ElMasria.Infrastructure.Repositories;
using ElMasria.Domain.Entities;
using ElMasria.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ElMasria.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure layer services: DbContext, Identity, Redis, repositories, UoW.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.MinBatchSize(5);
                    sqlOptions.MaxBatchSize(100);
                }));

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
                options.InstanceName = configuration.GetValue<string>("RedisSettings:InstanceName") ?? "ElMasria_";
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
        services.AddScoped<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, Services.ProductService>();
        services.AddScoped<IPhotoService, Services.PhotoService>();

        return services;
    }
}
