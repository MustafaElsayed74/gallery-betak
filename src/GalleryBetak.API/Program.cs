using AspNetCoreRateLimit;
using GalleryBetak.API.Extensions;
using GalleryBetak.API.Middleware;
using GalleryBetak.Application;
using GalleryBetak.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

// ── Bootstrap Serilog for startup logging ────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting GalleryBetak E-Commerce API...");

    var builder = WebApplication.CreateBuilder(args);

    var useInMemoryDatabase = builder.Configuration.GetValue<bool>("Database:UseInMemoryDatabase");
    if (useInMemoryDatabase && !builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Database:UseInMemoryDatabase=true is only allowed in Development. " +
            "Configure a persistent SQL Server connection for non-development environments.");
    }

    // ── Serilog ──────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, loggerConfig) =>
        loggerConfig.ReadFrom.Configuration(context.Configuration));

    // ================================================================
    // SERVICE REGISTRATION (order matters for dependencies)
    // ================================================================

    // 1. Infrastructure services (DbContext, Identity, Redis, Repos)
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // 2. Application services (AutoMapper, Validators, Services)
    builder.Services.AddApplicationServices();

    // 3. JWT Authentication + Authorization policies
    builder.Services.AddJwtAuthentication(builder.Configuration);

    // 4. CORS
    builder.Services.AddProductionCors(builder.Configuration);

    // 5. Rate Limiting
    builder.Services.AddApiRateLimiting(builder.Configuration);

    // 6. API Controllers + JSON configuration
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy =
                System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // 7. HttpContext accessor (needed by CurrentUserService)
    builder.Services.AddHttpContextAccessor();

    // 8. Swagger (Development only is controlled below in pipeline)
    builder.Services.AddSwaggerWithJwt();

    // 9. Health checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<GalleryBetak.Infrastructure.Data.AppDbContext>("sql-server");

    // 10. Response compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    // ================================================================
    // BUILD THE APP
    // ================================================================
    var app = builder.Build();

    // ── Seed Roles & Admin User ──────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<GalleryBetak.Infrastructure.Data.AppDbContext>();
        if (context.Database.IsRelational())
        {
            var hasMigrations = context.Database.GetMigrations().Any();
            if (hasMigrations)
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            // Safety net: ensure schema exists even when migrations are missing or database is newly provisioned.
            await context.Database.EnsureCreatedAsync();

            // SQL Server can report success from EnsureCreated when partial tables already exist.
            // For local development, recover by rebuilding schema if a core table is still missing.
            var categoriesTableExists = true;
            try
            {
                await context.Database.ExecuteSqlRawAsync("SELECT TOP (1) 1 FROM [Categories]");
            }
            catch
            {
                categoriesTableExists = false;
            }

            if (!categoriesTableExists && app.Environment.IsDevelopment())
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
            }
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }
        await GalleryBetak.Infrastructure.Data.AppDbContextSeeder.SeedAsync(context);
        
        await GalleryBetak.Infrastructure.Identity.IdentitySeeder.SeedAsync(services);
    }

    // ================================================================
    // MIDDLEWARE PIPELINE (order is CRITICAL for security)
    // ================================================================

    // 1. Global exception handler (catches all unhandled exceptions)
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // 2. Security headers (HSTS, CSP, X-Frame-Options, etc.)
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // 3. Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("UserId",
                httpContext.User.FindFirst("sub")?.Value ?? "anonymous");
            diagnosticContext.Set("ClientIp",
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        };
    });

    // 4. Swagger (Development only)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "GalleryBetak API v1");
            options.RoutePrefix = "swagger";
        });
    }

    // 5. Response compression
    app.UseResponseCompression();

    // 6. CORS (must come before auth)
    app.UseCors(CorsExtensions.PolicyName);

    // 7. Rate limiting
    app.UseIpRateLimiting();

    // 8. Authentication (who are you?)
    app.UseAuthentication();

    // 9. Authorization (what can you do?)
    app.UseAuthorization();

    // 10. Static files (for uploaded images)
    app.UseStaticFiles();

    // 11. Map endpoints
    app.MapControllers();

    // 12. Health checks
    app.MapHealthChecks("/health");

    // ================================================================
    // RUN
    // ================================================================
    Log.Information("GalleryBetak API started successfully. Environment: {Env}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "GalleryBetak API failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;

