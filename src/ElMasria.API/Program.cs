using AspNetCoreRateLimit;
using ElMasria.API.Extensions;
using ElMasria.API.Middleware;
using ElMasria.Application;
using ElMasria.Infrastructure;
using Serilog;

// ── Bootstrap Serilog for startup logging ────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ElMasria E-Commerce API...");

    var builder = WebApplication.CreateBuilder(args);

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
        .AddDbContextCheck<ElMasria.Infrastructure.Data.AppDbContext>("sql-server");

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
    await ElMasria.Infrastructure.Identity.IdentitySeeder.SeedAsync(app.Services);

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
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "ElMasria API v1");
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
    Log.Information("ElMasria API started successfully. Environment: {Env}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ElMasria API failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
