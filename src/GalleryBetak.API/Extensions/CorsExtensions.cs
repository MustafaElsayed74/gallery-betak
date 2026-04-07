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
        var configuredOrigins = configuration
            .GetSection("CorsSettings:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:4200"];

        var normalizedOrigins = configuredOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var explicitAllowedOrigins = normalizedOrigins
            .Where(origin => !origin.Contains('*'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allowVercelPreviewOrigins = normalizedOrigins
            .Any(origin => origin.Equals("https://*.vercel.app", StringComparison.OrdinalIgnoreCase));

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                      {
                          if (string.IsNullOrWhiteSpace(origin))
                          {
                              return false;
                          }

                          var normalizedOrigin = origin.Trim().TrimEnd('/');
                          if (explicitAllowedOrigins.Contains(normalizedOrigin))
                          {
                              return true;
                          }

                          if (!allowVercelPreviewOrigins)
                          {
                              return false;
                          }

                          if (!Uri.TryCreate(normalizedOrigin, UriKind.Absolute, out var uri))
                          {
                              return false;
                          }

                          return uri.Scheme == Uri.UriSchemeHttps
                              && uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase);
                      })
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()
                      .WithExposedHeaders("Token-Expired", "X-Session-Id", "X-Pagination");
            });
        });

        return services;
    }
}

