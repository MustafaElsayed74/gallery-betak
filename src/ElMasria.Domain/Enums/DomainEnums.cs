namespace ElMasria.Domain.Enums;

/// <summary>Order lifecycle status.</summary>
public enum OrderStatus
{
    /// <summary>Order created, awaiting payment or confirmation.</summary>
    Pending = 0,
    /// <summary>Payment received or admin confirmed.</summary>
    Confirmed = 1,
    /// <summary>Order being prepared for shipping.</summary>
    Processing = 2,
    /// <summary>Order shipped to customer.</summary>
    Shipped = 3,
    /// <summary>Order delivered to customer.</summary>
    Delivered = 4,
    /// <summary>Order cancelled by customer or admin.</summary>
    Cancelled = 5,
    /// <summary>Order refunded after delivery.</summary>
    Refunded = 6
}

/// <summary>Available payment methods for Egyptian market.</summary>
public enum PaymentMethod
{
    /// <summary>Vodafone Cash mobile wallet.</summary>
    VodafoneCash = 0,
    /// <summary>Orange Cash mobile wallet.</summary>
    OrangeCash = 1,
    /// <summary>Etisalat (e&) Cash mobile wallet.</summary>
    EtisalatCash = 2,
    /// <summary>Fawry payment at retail outlets.</summary>
    Fawry = 3,
    /// <summary>Cash on Delivery.</summary>
    COD = 4,
    /// <summary>Credit/Debit card via Paymob.</summary>
    Card = 5
}

/// <summary>Payment transaction status.</summary>
public enum PaymentStatus
{
    /// <summary>Payment initiated, awaiting completion.</summary>
    Pending = 0,
    /// <summary>Payment being processed by gateway.</summary>
    Processing = 1,
    /// <summary>Payment completed successfully.</summary>
    Success = 2,
    /// <summary>Payment failed.</summary>
    Failed = 3,
    /// <summary>Payment refunded.</summary>
    Refunded = 4
}

/// <summary>Coupon discount calculation type.</summary>
public enum DiscountType
{
    /// <summary>Percentage off the order total.</summary>
    Percentage = 0,
    /// <summary>Fixed amount off the order total (in EGP).</summary>
    FixedAmount = 1
}

/// <summary>Product review moderation status.</summary>
public enum ReviewStatus
{
    /// <summary>Review awaiting admin approval.</summary>
    Pending = 0,
    /// <summary>Review approved and visible.</summary>
    Approved = 1,
    /// <summary>Review rejected by admin.</summary>
    Rejected = 2
}
