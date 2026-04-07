using System.Security.Claims;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Order;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GalleryBetak.API.Controllers;

/// <summary>
/// Endpoints for order management.
/// </summary>
[Authorize]
public class OrdersController : BaseApiController
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Creates an order from the active cart.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var result = await _orderService.CreateOrderAsync(GetUserId(), request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Retrieves a specific order.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrder(int id)
    {
        var result = await _orderService.GetOrderByIdAsync(id, GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Retrieves all orders for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders()
    {
        var result = await _orderService.GetUserOrdersAsync(GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Admin: Updates order state machine.</summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] OrderStatus status, [FromQuery] string? trackingNumber, [FromQuery] string? reason)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, status, trackingNumber, reason);
        return StatusCode(result.StatusCode, result);
    }
}

