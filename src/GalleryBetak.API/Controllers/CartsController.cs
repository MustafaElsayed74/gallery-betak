using System.Security.Claims;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Cart;
using GalleryBetak.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GalleryBetak.API.Controllers;

/// <summary>
/// Handles shopping cart operations for both guests and authenticated users.
/// </summary>
public class CartsController : BaseApiController
{
    private readonly ICartService _cartService;
    private const string GuestSessionHeader = "X-Guest-Session-Id";

    public CartsController(ICartService cartService)
    {
        _cartService = cartService;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? GetSessionId() => Request.Headers[GuestSessionHeader].FirstOrDefault();

    /// <summary>Gets the current user's or guest's cart.</summary>
    /// <response code="200">Returns the cart and its contents.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart()
    {
        if (GetUserId() == null && GetSessionId() == null)
            return BadRequest(ApiResponse<object>.Fail(400, "يجب تحديد مستخدم أو جلسة", "Missing User/Session ID"));

        var result = await _cartService.GetCartAsync(GetUserId(), GetSessionId());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Adds a product to the cart.</summary>
    /// <response code="200">Cart updated successfully.</response>
    [HttpPost("items")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        if (GetUserId() == null && GetSessionId() == null)
            return BadRequest(ApiResponse<object>.Fail(400, "يجب تحديد مستخدم أو جلسة", "Missing User/Session ID"));

        var result = await _cartService.AddToCartAsync(GetUserId(), GetSessionId(), request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Updates cart item quantity.</summary>
    /// <response code="200">Cart updated successfully.</response>
    [HttpPut("items/{productId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItem(int productId, [FromBody] UpdateCartItemRequest request)
    {
        var result = await _cartService.UpdateItemQuantityAsync(GetUserId(), GetSessionId(), productId, request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Removes an item from the cart.</summary>
    /// <response code="200">Cart updated successfully.</response>
    [HttpDelete("items/{productId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var result = await _cartService.RemoveItemAsync(GetUserId(), GetSessionId(), productId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Clears the cart.</summary>
    /// <response code="200">Cart cleared successfully.</response>
    [HttpDelete]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearCart()
    {
        var result = await _cartService.ClearCartAsync(GetUserId(), GetSessionId());
        return StatusCode(result.StatusCode, result);
    }
}

