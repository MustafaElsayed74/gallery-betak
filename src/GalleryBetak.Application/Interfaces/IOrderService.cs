using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Order;
using GalleryBetak.Domain.Enums;

namespace GalleryBetak.Application.Interfaces;

/// <summary>
/// Service contract for atomic order processing.
/// </summary>
public interface IOrderService
{
    /// <summary>Creates a new order from the active user's cart.</summary>
    Task<ApiResponse<OrderDto>> CreateOrderAsync(string userId, CreateOrderRequest request, CancellationToken ct = default);

    /// <summary>Retrieves a specific order for a user.</summary>
    Task<ApiResponse<OrderDto>> GetOrderByIdAsync(int id, string userId, CancellationToken ct = default);

    /// <summary>Retrieves all orders for a user.</summary>
    Task<ApiResponse<IReadOnlyList<OrderSummaryDto>>> GetUserOrdersAsync(string userId, CancellationToken ct = default);

    /// <summary>Admin: Updates order status.</summary>
    Task<ApiResponse<bool>> UpdateOrderStatusAsync(int id, OrderStatus newStatus, string? trackingNumber = null, string? cancelReason = null, CancellationToken ct = default);
}

