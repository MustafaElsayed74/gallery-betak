using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Wishlist;

namespace GalleryBetak.Application.Interfaces;

/// <summary>
/// Service contract for managing user wishlists.
/// </summary>
public interface IWishlistService
{
    /// <summary>Gets the user's wishlist.</summary>
    Task<ApiResponse<WishlistDto>> GetWishlistAsync(string userId, CancellationToken ct = default);

    /// <summary>Toggles a product in the wishlist (adds if missing, removes if present).</summary>
    Task<ApiResponse<WishlistDto>> ToggleWishlistAsync(string userId, int productId, CancellationToken ct = default);

    /// <summary>Clears the entire wishlist.</summary>
    Task<ApiResponse<bool>> ClearWishlistAsync(string userId, CancellationToken ct = default);

    /// <summary>Moves a wishlist item directly into the active cart.</summary>
    Task<ApiResponse<WishlistDto>> MoveToCartAsync(string userId, int productId, string? sessionId = null, CancellationToken ct = default);
}

