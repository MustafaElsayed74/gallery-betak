using System.Security.Claims;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Wishlist;
using GalleryBetak.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GalleryBetak.API.Controllers;

/// <summary>
/// Handles user wishlists (favorites).
/// Requires authentication.
/// </summary>
[Authorize]
public class WishlistsController : BaseApiController
{
    private readonly IWishlistService _wishlistService;
    private const string GuestSessionHeader = "X-Guest-Session-Id";

    public WishlistsController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string? GetSessionId() => Request.Headers[GuestSessionHeader].FirstOrDefault();

    /// <summary>Gets the current user's wishlist.</summary>
    /// <response code="200">Returns the wishlist items.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWishlist()
    {
        var result = await _wishlistService.GetWishlistAsync(GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Toggles a product in the wishlist (Add/Remove).</summary>
    /// <response code="200">Wishlist updated successfully.</response>
    [HttpPost("toggle/{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleItem(int productId)
    {
        var result = await _wishlistService.ToggleWishlistAsync(GetUserId(), productId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Clears the wishlist.</summary>
    /// <response code="200">Wishlist cleared successfully.</response>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearWishlist()
    {
        var result = await _wishlistService.ClearWishlistAsync(GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Moves a wishlist item to the active cart.</summary>
    /// <response code="200">Product moved successfully.</response>
    [HttpPost("move-to-cart/{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveToCart(int productId)
    {
        var result = await _wishlistService.MoveToCartAsync(GetUserId(), productId, GetSessionId());
        return StatusCode(result.StatusCode, result);
    }
}

