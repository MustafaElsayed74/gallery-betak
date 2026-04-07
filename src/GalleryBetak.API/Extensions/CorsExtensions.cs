namespace GalleryBetak.API.Extensions;

/// <summary>
/// Configures CORS policies for the API.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Name of the production CORS policy.
    /// </summary>
    public const string PolicyName = "GalleryBetakCorsPolicy";

    /// <summary>
    /// Adds CORS with allowed origins from configuration.
    /// </summary>
    public static IServiceCollection AddProductionCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("CorsSettings:AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:4200" };

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()
                      .WithExposedHeaders("Token-Expired", "X-Session-Id", "X-Pagination");
            });
        });

        return services;
    }
}

