using GalleryBetak.Application.Common;
using GalleryBetak.Application.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace GalleryBetak.Application;

/// <summary>
/// Extension methods for registering Application layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Application layer services: AutoMapper, FluentValidation, application services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // AutoMapper — scans this assembly for Profile classes
        services.AddAutoMapper(assembly);

        // FluentValidation — scans this assembly for AbstractValidator<T> implementations
        services.AddValidatorsFromAssembly(assembly);

        // Application services will be registered here as they are implemented
        // Example:
        // services.AddScoped<IProductService, ProductService>();
        // services.AddScoped<ICategoryService, CategoryService>();
        // services.AddScoped<IOrderService, OrderService>();
        // services.AddScoped<ICartService, CartService>();
        // services.AddScoped<IWishlistService, WishlistService>();
        // services.AddScoped<ISearchService, SearchService>();
        // services.AddScoped<IReviewService, ReviewService>();

        return services;
    }
}

