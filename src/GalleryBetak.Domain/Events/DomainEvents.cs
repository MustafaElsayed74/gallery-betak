namespace GalleryBetak.Domain.Events;

/// <summary>
/// Base class for all domain events. Raised by entities and dispatched by DbContext.
/// </summary>
public abstract class BaseDomainEvent
{
    /// <summary>Unique event ID.</summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>UTC timestamp when the event occurred.</summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>Raised when a new order is placed successfully.</summary>
public sealed class OrderPlacedEvent : BaseDomainEvent
{
    /// <summary>The order ID.</summary>
    public int OrderId { get; }
    /// <summary>The formatted order number.</summary>
    public string OrderNumber { get; }
    /// <summary>The customer user ID.</summary>
    public string UserId { get; }
    /// <summary>Total order amount in EGP.</summary>
    public decimal TotalAmount { get; }

    /// <summary>Creates an OrderPlacedEvent.</summary>
    public OrderPlacedEvent(int orderId, string orderNumber, string userId, decimal totalAmount)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        UserId = userId;
        TotalAmount = totalAmount;
    }
}

/// <summary>Raised when an order is shipped.</summary>
public sealed class OrderShippedEvent : BaseDomainEvent
{
    /// <summary>The order ID.</summary>
    public int OrderId { get; }
    /// <summary>Shipping tracking number.</summary>
    public string TrackingNumber { get; }

    /// <summary>Creates an OrderShippedEvent.</summary>
    public OrderShippedEvent(int orderId, string trackingNumber)
    {
        OrderId = orderId;
        TrackingNumber = trackingNumber;
    }
}

/// <summary>Raised when an order is cancelled.</summary>
public sealed class OrderCancelledEvent : BaseDomainEvent
{
    /// <summary>The order ID.</summary>
    public int OrderId { get; }
    /// <summary>Reason for cancellation.</summary>
    public string Reason { get; }

    /// <summary>Creates an OrderCancelledEvent.</summary>
    public OrderCancelledEvent(int orderId, string reason)
    {
        OrderId = orderId;
        Reason = reason;
    }
}

/// <summary>Raised when a payment is confirmed by the gateway.</summary>
public sealed class PaymentConfirmedEvent : BaseDomainEvent
{
    /// <summary>The order ID.</summary>
    public int OrderId { get; }
    /// <summary>Payment amount in EGP.</summary>
    public decimal Amount { get; }
    /// <summary>Gateway transaction ID.</summary>
    public string TransactionId { get; }

    /// <summary>Creates a PaymentConfirmedEvent.</summary>
    public PaymentConfirmedEvent(int orderId, decimal amount, string transactionId)
    {
        OrderId = orderId;
        Amount = amount;
        TransactionId = transactionId;
    }
}

/// <summary>Raised when product stock drops below threshold (5 units).</summary>
public sealed class ProductStockLowEvent : BaseDomainEvent
{
    /// <summary>The product ID.</summary>
    public int ProductId { get; }
    /// <summary>Product name in Arabic.</summary>
    public string ProductNameAr { get; }
    /// <summary>Current remaining stock quantity.</summary>
    public int RemainingStock { get; }

    /// <summary>Creates a ProductStockLowEvent.</summary>
    public ProductStockLowEvent(int productId, string productNameAr, int remainingStock)
    {
        ProductId = productId;
        ProductNameAr = productNameAr;
        RemainingStock = remainingStock;
    }
}

