namespace ElMasria.Domain.Exceptions;

/// <summary>
/// Base domain exception. All domain-specific exceptions inherit from this.
/// </summary>
public class DomainException : Exception
{
    /// <summary>English message for server-side logging.</summary>
    public string? MessageEn { get; }

    /// <summary>
    /// Creates a domain exception with Arabic (primary) and English messages.
    /// </summary>
    /// <param name="messageAr">Arabic user-facing message.</param>
    /// <param name="messageEn">English message for logging.</param>
    public DomainException(string messageAr, string? messageEn = null) : base(messageAr)
    {
        MessageEn = messageEn;
    }
}

/// <summary>Thrown when a requested resource is not found.</summary>
public class NotFoundException : DomainException
{
    /// <summary>Creates a not found exception.</summary>
    public NotFoundException(string messageAr, string? messageEn = null) : base(messageAr, messageEn) { }

    /// <summary>Creates a not found exception for a specific entity type and ID.</summary>
    public NotFoundException(string entityName, object id)
        : base($"لم يتم العثور على {entityName} بالمعرف {id}", $"{entityName} with ID {id} not found.") { }
}

/// <summary>Thrown when the user lacks permission.</summary>
public class UnauthorizedException : DomainException
{
    /// <summary>Creates an unauthorized exception.</summary>
    public UnauthorizedException(string messageAr = "غير مصرح لك بتنفيذ هذا الإجراء", string? messageEn = "Access denied.")
        : base(messageAr, messageEn) { }
}

/// <summary>Thrown when a business rule is violated.</summary>
public class BusinessRuleException : DomainException
{
    /// <summary>Creates a business rule exception.</summary>
    public BusinessRuleException(string messageAr, string? messageEn = null) : base(messageAr, messageEn) { }
}

/// <summary>Thrown when there is insufficient stock for an operation.</summary>
public class InsufficientStockException : DomainException
{
    /// <summary>Available quantity at the time of the exception.</summary>
    public int AvailableQuantity { get; }

    /// <summary>Requested quantity that exceeded stock.</summary>
    public int RequestedQuantity { get; }

    /// <summary>Creates an insufficient stock exception.</summary>
    public InsufficientStockException(int available, int requested)
        : base($"الكمية المطلوبة ({requested}) غير متوفرة. الكمية المتاحة: {available}",
               $"Insufficient stock. Available: {available}, Requested: {requested}.")
    {
        AvailableQuantity = available;
        RequestedQuantity = requested;
    }
}

/// <summary>Thrown when attempting to use an expired coupon.</summary>
public class CouponExpiredException : DomainException
{
    /// <summary>Creates a coupon expired exception.</summary>
    public CouponExpiredException(string couponCode)
        : base($"كوبون الخصم '{couponCode}' منتهي الصلاحية",
               $"Coupon '{couponCode}' has expired.") { }
}

/// <summary>Thrown when attempting to reuse a coupon beyond its limit.</summary>
public class CouponAlreadyUsedException : DomainException
{
    /// <summary>Creates a coupon already used exception.</summary>
    public CouponAlreadyUsedException(string couponCode)
        : base($"تم استخدام كوبون الخصم '{couponCode}' بالفعل",
               $"Coupon '{couponCode}' has already been used.") { }
}
