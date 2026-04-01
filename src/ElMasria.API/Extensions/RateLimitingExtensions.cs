using AspNetCoreRateLimit;

namespace ElMasria.API.Extensions;

/// <summary>
/// Configures API rate limiting per endpoint type.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Adds IP-based rate limiting with per-endpoint rules.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Load rate limit options from configuration or use defaults
        services.AddMemoryCache();

        services.Configure<IpRateLimitOptions>(options =>
        {
            options.EnableEndpointRateLimiting = true;
            options.StackBlockedRequests = false;
            options.HttpStatusCode = 429;
            options.RealIpHeader = "X-Forwarded-For";
            options.ClientIdHeader = "X-ClientId";

            options.GeneralRules = new List<RateLimitRule>
            {
                // General API: 100 requests per minute
                new()
                {
                    Endpoint = "*",
                    Period = "1m",
                    Limit = 100
                },
                // Auth login: 5 per minute (brute force protection)
                new()
                {
                    Endpoint = "post:/api/v1/auth/login",
                    Period = "1m",
                    Limit = 5
                },
                // Auth register: 3 per minute
                new()
                {
                    Endpoint = "post:/api/v1/auth/register",
                    Period = "1m",
                    Limit = 3
                },
                // Password reset: 3 per hour per IP
                new()
                {
                    Endpoint = "post:/api/v1/auth/forgot-password",
                    Period = "1h",
                    Limit = 3
                },
                // Search autocomplete: 60 per minute
                new()
                {
                    Endpoint = "get:/api/v1/search/autocomplete*",
                    Period = "1m",
                    Limit = 60
                }
            };

            options.QuotaExceededResponse = new QuotaExceededResponse
            {
                Content = "{\"success\":false,\"statusCode\":429,\"message\":\"تم تجاوز الحد المسموح من الطلبات. حاول مرة أخرى بعد قليل.\",\"messageEn\":\"Rate limit exceeded. Please try again later.\",\"data\":null,\"errors\":[]}",
                ContentType = "application/json; charset=utf-8",
                StatusCode = 429
            };
        });

        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddInMemoryRateLimiting();

        return services;
    }
}
