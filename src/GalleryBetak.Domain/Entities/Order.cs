using GalleryBetak.Domain.Enums;
using GalleryBetak.Domain.Events;

namespace GalleryBetak.Domain.Entities;

/// <summary>
/// Customer order with frozen address snapshot, state machine transitions,
/// and domain event raising. Contains full order lifecycle management.
/// </summary>
public sealed class Order : BaseEntity
{
    /// <summary>Formatted order number (ORD-YYYYMM-NNNNN).</summary>
    public string OrderNumber { get; private set; } = string.Empty;

    /// <summary>Customer user ID. SET NULL on user deletion (financial records preserved).</summary>
    public string? UserId { get; private set; }

    /// <summary>Current order status.</summary>
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;

    /// <summary>Subtotal before shipping, tax, and discounts.</summary>
    public decimal SubTotal { get; private set; }

    /// <summary>Shipping cost in EGP.</summary>
    public decimal ShippingCost { get; private set; }

    /// <summary>Tax amount.</summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>Discount amount applied via coupon.</summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>Final total = SubTotal + Shipping + Tax - Discount.</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>Applied coupon ID.</summary>
    public int? CouponId { get; private set; }

    /// <summary>Coupon code snapshot (preserved after coupon deletion).</summary>
    public string? CouponCode { get; private set; }

    /// <summary>Payment method used.</summary>
    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>Payment status.</summary>
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Pending;

    /// <summary>Customer notes.</summary>
    public string? Notes { get; private set; }

    // ── Shipping Address Snapshot ──────────────────────────────────
    /// <summary>Recipient name at order time.</summary>
    public string ShippingRecipientName { get; private set; } = string.Empty;
    /// <summary>Phone at order time.</summary>
    public string ShippingPhone { get; private set; } = string.Empty;
    /// <summary>Governorate at order time.</summary>
    public string ShippingGovernorate { get; private set; } = string.Empty;
    /// <summary>City at order time.</summary>
    public string ShippingCity { get; private set; } = string.Empty;
    /// <summary>District at order time.</summary>
    public string? ShippingDistrict { get; private set; }
    /// <summary>Street address at order time.</summary>
    public string ShippingStreetAddress { get; private set; } = string.Empty;
    /// <summary>Building number at order time.</summary>
    public string? ShippingBuildingNo { get; private set; }
    /// <summary>Apartment number at order time.</summary>
    public string? ShippingApartmentNo { get; private set; }
    /// <summary>Postal code at order time.</summary>
    public string? ShippingPostalCode { get; private set; }

    // ── Shipping Tracking ─────────────────────────────────────────
    /// <summary>Tracking number from shipping provider.</summary>
    public string? TrackingNumber { get; private set; }
    /// <summary>When the order was shipped.</summary>
    public DateTime? ShippedAt { get; private set; }
    /// <summary>When the order was delivered.</summary>
    public DateTime? DeliveredAt { get; private set; }
    /// <summary>When the order was cancelled.</summary>
    public DateTime? CancelledAt { get; private set; }
    /// <summary>Cancellation reason.</summary>
    public string? CancellationReason { get; private set; }

    // ── Domain Events ─────────────────────────────────────────────
    private readonly List<BaseDomainEvent> _domainEvents = new();
    /// <summary>Pending domain events.</summary>
    public IReadOnlyList<BaseDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    /// <summary>Clears domain events after dispatch.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    // ── Navigation ────────────────────────────────────────────────
    /// <summary>Order line items.</summary>
    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    /// <summary>Payment records.</summary>
    public ICollection<Payment> Payments { get; private set; } = new List<Payment>();

    private Order() { }

    /// <summary>
    /// Factory method to create a new order with address snapshot from an Address entity.
    /// </summary>
    public static Order Create(string userId, string orderNumber, Address shippingAddress,
        PaymentMethod paymentMethod, string? notes = null)
    {
        var order = new Order
        {
            UserId = userId,
            OrderNumber = orderNumber,
            PaymentMethod = paymentMethod,
            Notes = notes,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,

            // Freeze address snapshot
            ShippingRecipientName = shippingAddress.RecipientName,
            ShippingPhone = shippingAddress.Phone,
            ShippingGovernorate = shippingAddress.Governorate,
            ShippingCity = shippingAddress.City,
            ShippingDistrict = shippingAddress.District,
            ShippingStreetAddress = shippingAddress.StreetAddress,
            ShippingBuildingNo = shippingAddress.BuildingNo,
            ShippingApartmentNo = shippingAddress.ApartmentNo,
            ShippingPostalCode = shippingAddress.PostalCode
        };

        return order;
    }

    /// <summary>Adds a line item to the order.</summary>
    public void AddItem(OrderItem item)
    {
        Items.Add(item);
        RecalculateTotals();
    }

    /// <summary>Sets financial details (called during order creation).</summary>
    public void SetFinancials(decimal shippingCost, decimal taxAmount, decimal discountAmount,
        int? couponId = null, string? couponCode = null)
    {
        ShippingCost = shippingCost;
        TaxAmount = taxAmount;
        DiscountAmount = discountAmount;
        CouponId = couponId;
        CouponCode = couponCode;
        RecalculateTotals();
    }

    /// <summary>Recalculates subtotal and total from items.</summary>
    public void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.TotalPrice);
        TotalAmount = SubTotal + ShippingCost + TaxAmount - DiscountAmount;
        if (TotalAmount < 0) TotalAmount = 0;
    }

    /// <summary>Finalizes order placement and raises OrderPlacedEvent.</summary>
    public void PlaceOrder()
    {
        if (!Items.Any())
            throw new Exceptions.BusinessRuleException("لا يمكن تقديم طلب بدون منتجات", "Cannot place an empty order.");
        if (TotalAmount <= 0)
            throw new Exceptions.BusinessRuleException("إجمالي الطلب غير صالح", "Invalid order total.");

        _domainEvents.Add(new OrderPlacedEvent(Id, OrderNumber, UserId ?? "", TotalAmount));
    }

    // ── State Transitions ─────────────────────────────────────────

    /// <summary>Confirms the order (by admin or auto after payment).</summary>
    public void Confirm()
    {
        ValidateTransition(OrderStatus.Confirmed);
        Status = OrderStatus.Confirmed;
    }

    /// <summary>Moves order to processing.</summary>
    public void StartProcessing()
    {
        ValidateTransition(OrderStatus.Processing);
        Status = OrderStatus.Processing;
    }

    /// <summary>Ships the order with a tracking number.</summary>
    public void Ship(string trackingNumber)
    {
        ValidateTransition(OrderStatus.Shipped);
        Status = OrderStatus.Shipped;
        TrackingNumber = trackingNumber;
        ShippedAt = DateTime.UtcNow;

        _domainEvents.Add(new OrderShippedEvent(Id, trackingNumber));
    }

    /// <summary>Marks the order as delivered.</summary>
    public void Deliver()
    {
        ValidateTransition(OrderStatus.Delivered);
        Status = OrderStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
    }

    /// <summary>Cancels the order with a reason.</summary>
    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Refunded)
            throw new Exceptions.BusinessRuleException("لا يمكن إلغاء الطلب في هذه الحالة", "Cannot cancel order in current state.");

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;

        _domainEvents.Add(new OrderCancelledEvent(Id, reason));
    }

    /// <summary>Marks the order as refunded.</summary>
    public void Refund()
    {
        if (Status != OrderStatus.Delivered)
            throw new Exceptions.BusinessRuleException("لا يمكن استرجاع طلب غير مسلم", "Can only refund delivered orders.");
        Status = OrderStatus.Refunded;
    }

    /// <summary>Updates the payment status.</summary>
    public void UpdatePaymentStatus(PaymentStatus status) => PaymentStatus = status;

    private void ValidateTransition(OrderStatus target)
    {
        var valid = (Status, target) switch
        {
            (OrderStatus.Pending, OrderStatus.Confirmed) => true,
            (OrderStatus.Confirmed, OrderStatus.Processing) => true,
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false
        };

        if (!valid)
            throw new Exceptions.BusinessRuleException(
                $"لا يمكن الانتقال من {Status} إلى {target}",
                $"Invalid transition from {Status} to {target}.");
    }
}

/// <summary>
/// Order line item with frozen product snapshot.
/// </summary>
public sealed class OrderItem
{
    /// <summary>Primary key.</summary>
    public int Id { get; private set; }

    /// <summary>Order ID.</summary>
    public int OrderId { get; private set; }

    /// <summary>Product ID (SET NULL on product deletion, snapshot preserved).</summary>
    public int? ProductId { get; private set; }

    /// <summary>Product name in Arabic (snapshot).</summary>
    public string ProductNameAr { get; private set; } = string.Empty;

    /// <summary>Product name in English (snapshot).</summary>
    public string ProductNameEn { get; private set; } = string.Empty;

    /// <summary>Product SKU (snapshot).</summary>
    public string ProductSKU { get; private set; } = string.Empty;

    /// <summary>Product image URL (snapshot).</summary>
    public string? ProductImageUrl { get; private set; }

    /// <summary>Unit price frozen at order time.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Ordered quantity.</summary>
    public int Quantity { get; private set; }

    /// <summary>Line total = UnitPrice × Quantity.</summary>
    public decimal TotalPrice { get; private set; }

    // Navigation
    /// <summary>The order.</summary>
    public Order Order { get; private set; } = null!;

    /// <summary>The product reference (may be null if product deleted).</summary>
    public Product? Product { get; private set; }

    private OrderItem() { }

    /// <summary>Creates an order item with a product snapshot.</summary>
    public static OrderItem Create(Product product, int quantity)
    {
        if (quantity <= 0)
            throw new Exceptions.DomainException("الكمية يجب أن تكون أكبر من صفر", "Quantity must be > 0.");

        var primaryImage = product.Images.FirstOrDefault(i => i.IsPrimary)
                        ?? product.Images.FirstOrDefault();

        return new OrderItem
        {
            ProductId = product.Id,
            ProductNameAr = product.NameAr,
            ProductNameEn = product.NameEn,
            ProductSKU = product.SKU,
            ProductImageUrl = primaryImage?.ThumbnailUrl ?? primaryImage?.ImageUrl,
            UnitPrice = product.Price,
            Quantity = quantity,
            TotalPrice = product.Price * quantity
        };
    }
}

