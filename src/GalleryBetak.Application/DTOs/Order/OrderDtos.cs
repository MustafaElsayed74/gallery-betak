using System.ComponentModel.DataAnnotations;
using GalleryBetak.Domain.Enums;

namespace GalleryBetak.Application.DTOs.Order;

/// <summary>Request to convert the active cart into a placed order.</summary>
public sealed record CreateOrderRequest
{
    [Required]
    public int AddressId { get; init; }

    [Required]
    public PaymentMethod PaymentMethod { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}

/// <summary>Full order details response.</summary>
public sealed record OrderDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal SubTotal { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }

    // Shipping Snapshot
    public string ShippingRecipientName { get; init; } = string.Empty;
    public string ShippingPhone { get; init; } = string.Empty;
    public string ShippingGovernorate { get; init; } = string.Empty;
    public string ShippingCity { get; init; } = string.Empty;
    public string ShippingStreetAddress { get; init; } = string.Empty;
    public string? TrackingNumber { get; init; }

    public IReadOnlyList<OrderItemDto> Items { get; init; } = [];
}

/// <summary>Summary order response for lists.</summary>
public sealed record OrderSummaryDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public int ItemCount { get; init; }
}

/// <summary>Detailed order item.</summary>
public sealed record OrderItemDto
{
    public int ProductId { get; init; }
    public string ProductNameAr { get; init; } = string.Empty;
    public string ProductNameEn { get; init; } = string.Empty;
    public string ProductSKU { get; init; } = string.Empty;
    public string? ProductImageUrl { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal TotalPrice { get; init; }
}

