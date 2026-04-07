using Microsoft.OpenApi.Models;

namespace GalleryBetak.API.Extensions;

/// <summary>
/// Configures Swagger/OpenAPI with JWT bearer authentication support.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Adds Swagger generation with JWT security definition.
    /// </summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "GalleryBetak E-Commerce API — واجهة برمجة متجر المصرية",
                Version = "v1",
                Description = "Production-grade Arabic e-commerce API with Egyptian market support (EGP, governorates, mobile wallets).",
                Contact = new OpenApiContact
                {
                    Name = "GalleryBetak Support",
                    Email = "support@gallery-betak.com"
                }
            });

            // JWT Bearer auth in Swagger UI
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT token: Bearer {token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            options.AddSecurityDefinition("Bearer", securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }
            });

            // Include XML documentation
            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "GalleryBetak.*.xml", SearchOption.TopDirectoryOnly);
            foreach (var xmlFile in xmlFiles)
            {
                options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
            }

            // Enable annotations
            options.EnableAnnotations();
        });

        return services;
    }
}

