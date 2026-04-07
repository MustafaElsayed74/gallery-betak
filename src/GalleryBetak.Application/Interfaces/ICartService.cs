using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Cart;

namespace GalleryBetak.Application.Interfaces;

/// <summary>
/// Service contract for managing user and guest shopping carts.
/// </summary>
public interface ICartService
{
    /// <summary>Gets the cart for the user or session.</summary>
    Task<ApiResponse<CartDto>> GetCartAsync(string? userId, string? sessionId, CancellationToken ct = default);

    /// <summary>Adds a product to the cart.</summary>
    Task<ApiResponse<CartDto>> AddToCartAsync(string? userId, string? sessionId, AddToCartRequest request, CancellationToken ct = default);

    /// <summary>Updates the quantity of an item in the cart.</summary>
    Task<ApiResponse<CartDto>> UpdateItemQuantityAsync(string? userId, string? sessionId, int productId, UpdateCartItemRequest request, CancellationToken ct = default);

    /// <summary>Removes an item from the cart.</summary>
    Task<ApiResponse<CartDto>> RemoveItemAsync(string? userId, string? sessionId, int productId, CancellationToken ct = default);

    /// <summary>Clears all items from the cart.</summary>
    Task<ApiResponse<bool>> ClearCartAsync(string? userId, string? sessionId, CancellationToken ct = default);

    /// <summary>Merges a guest cart into a user cart (called upon login).</summary>
    Task<ApiResponse<bool>> MergeCartAsync(string userId, string sessionId, CancellationToken ct = default);
}

