using GalleryBetak.Domain.Enums;

namespace GalleryBetak.Domain.Entities;

/// <summary>
/// Discount coupon with flexible rules (percentage or fixed, usage limits, date range).
/// </summary>
public sealed class Coupon : BaseEntity
{
    /// <summary>Unique coupon code (e.g., "WELCOME10").</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Description in Arabic.</summary>
    public string? DescriptionAr { get; private set; }

    /// <summary>Description in English.</summary>
    public string? DescriptionEn { get; private set; }

    /// <summary>Discount type: Percentage or FixedAmount.</summary>
    public DiscountType DiscountType { get; private set; }

    /// <summary>Discount value (percentage 1-100 or fixed EGP amount).</summary>
    public decimal DiscountValue { get; private set; }

    /// <summary>Minimum order amount required to use coupon.</summary>
    public decimal MinOrderAmount { get; private set; }

    /// <summary>Maximum discount cap for percentage coupons (null = no cap).</summary>
    public decimal? MaxDiscountAmount { get; private set; }

    /// <summary>Maximum number of uses (0 = unlimited).</summary>
    public int UsageLimit { get; private set; }

    /// <summary>Number of times used.</summary>
    public int UsedCount { get; private set; }

    /// <summary>Coupon validity start date.</summary>
    public DateTime StartsAt { get; private set; }

    /// <summary>Coupon expiry date.</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Whether the coupon is active.</summary>
    public bool IsActive { get; private set; } = true;

    // Navigation
    /// <summary>Usage tracking records.</summary>
    public ICollection<CouponUsage> Usages { get; private set; } = new List<CouponUsage>();

    private Coupon() { }

    /// <summary>Creates a new coupon.</summary>
    public static Coupon Create(string code, DiscountType discountType, decimal discountValue,
        DateTime startsAt, DateTime expiresAt, decimal minOrderAmount = 0, decimal? maxDiscountAmount = null,
        int usageLimit = 0)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new Exceptions.DomainException("كود الكوبون مطلوب", "Coupon code is required.");
        if (discountType == DiscountType.Percentage && (discountValue <= 0 || discountValue > 100))
            throw new Exceptions.DomainException("نسبة الخصم يجب أن تكون بين 1 و 100", "Percentage must be between 1 and 100.");
        if (discountValue <= 0)
            throw new Exceptions.DomainException("قيمة الخصم يجب أن تكون أكبر من صفر", "Discount value must be > 0.");
        if (minOrderAmount < 0)
            throw new Exceptions.DomainException("الحد الأدنى للطلب لا يمكن أن يكون سالبًا", "Minimum order amount cannot be negative.");
        if (maxDiscountAmount.HasValue && maxDiscountAmount.Value <= 0)
            throw new Exceptions.DomainException("الحد الأقصى للخصم يجب أن يكون أكبر من صفر", "Maximum discount amount must be > 0.");
        if (usageLimit < 0)
            throw new Exceptions.DomainException("عدد مرات الاستخدام لا يمكن أن يكون سالبًا", "Usage limit cannot be negative.");
        if (expiresAt <= startsAt)
            throw new Exceptions.DomainException("تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية", "Expiry must be after start date.");

        return new Coupon
        {
            Code = code.ToUpperInvariant(),
            DiscountType = discountType,
            DiscountValue = discountValue,
            StartsAt = startsAt,
            ExpiresAt = expiresAt,
            MinOrderAmount = minOrderAmount,
            MaxDiscountAmount = maxDiscountAmount,
            UsageLimit = usageLimit,
            IsActive = true
        };
    }

    /// <summary>Sets optional coupon descriptions.</summary>
    public void SetDescriptions(string? descriptionAr, string? descriptionEn)
    {
        DescriptionAr = string.IsNullOrWhiteSpace(descriptionAr) ? null : descriptionAr.Trim();
        DescriptionEn = string.IsNullOrWhiteSpace(descriptionEn) ? null : descriptionEn.Trim();
    }

    /// <summary>Updates coupon rules and validity window.</summary>
    public void Update(string code, DiscountType discountType, decimal discountValue,
        DateTime startsAt, DateTime expiresAt, decimal minOrderAmount = 0, decimal? maxDiscountAmount = null,
        int usageLimit = 0)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new Exceptions.DomainException("كود الكوبون مطلوب", "Coupon code is required.");
        if (discountType == DiscountType.Percentage && (discountValue <= 0 || discountValue > 100))
            throw new Exceptions.DomainException("نسبة الخصم يجب أن تكون بين 1 و 100", "Percentage must be between 1 and 100.");
        if (discountValue <= 0)
            throw new Exceptions.DomainException("قيمة الخصم يجب أن تكون أكبر من صفر", "Discount value must be > 0.");
        if (minOrderAmount < 0)
            throw new Exceptions.DomainException("الحد الأدنى للطلب لا يمكن أن يكون سالبًا", "Minimum order amount cannot be negative.");
        if (maxDiscountAmount.HasValue && maxDiscountAmount.Value <= 0)
            throw new Exceptions.DomainException("الحد الأقصى للخصم يجب أن يكون أكبر من صفر", "Maximum discount amount must be > 0.");
        if (usageLimit < 0)
            throw new Exceptions.DomainException("عدد مرات الاستخدام لا يمكن أن يكون سالبًا", "Usage limit cannot be negative.");
        if (usageLimit > 0 && usageLimit < UsedCount)
            throw new Exceptions.DomainException("لا يمكن أن يكون حد الاستخدام أقل من عدد الاستخدامات الحالي", "Usage limit cannot be less than used count.");
        if (expiresAt <= startsAt)
            throw new Exceptions.DomainException("تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية", "Expiry must be after start date.");

        Code = code.ToUpperInvariant();
        DiscountType = discountType;
        DiscountValue = discountValue;
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
        MinOrderAmount = minOrderAmount;
        MaxDiscountAmount = maxDiscountAmount;
        UsageLimit = usageLimit;
    }

    /// <summary>Toggles coupon active state.</summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    /// <summary>Activates coupon usage.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Deactivates coupon usage.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Calculates the discount amount for a given order subtotal.</summary>
    public decimal CalculateDiscount(decimal orderSubTotal)
    {
        var discount = DiscountType == DiscountType.Percentage
            ? orderSubTotal * (DiscountValue / 100m)
            : DiscountValue;

        // Apply max discount cap for percentage coupons
        if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
            discount = MaxDiscountAmount.Value;

        // Cannot exceed order total
        return Math.Min(discount, orderSubTotal);
    }

    /// <summary>Validates and records a coupon usage. Throws if invalid.</summary>
    public void Use(string userId, int orderId)
    {
        if (!IsActive)
            throw new Exceptions.BusinessRuleException("هذا الكوبون غير نشط", "Coupon is not active.");
        if (DateTime.UtcNow < StartsAt)
            throw new Exceptions.BusinessRuleException("الكوبون لم يبدأ بعد", "Coupon has not started yet.");
        if (DateTime.UtcNow > ExpiresAt)
            throw new Exceptions.CouponExpiredException(Code);
        if (UsageLimit > 0 && UsedCount >= UsageLimit)
            throw new Exceptions.BusinessRuleException("تم استنفاد عدد استخدامات الكوبون", "Coupon usage limit reached.");

        UsedCount++;
    }

    /// <summary>Whether the coupon is currently valid for use.</summary>
    public bool IsValid => IsActive
        && DateTime.UtcNow >= StartsAt
        && DateTime.UtcNow <= ExpiresAt
        && (UsageLimit == 0 || UsedCount < UsageLimit);
}

/// <summary>Tracks individual coupon usage per user per order.</summary>
public sealed class CouponUsage
{
    /// <summary>Primary key.</summary>
    public int Id { get; private set; }

    /// <summary>Coupon ID.</summary>
    public int CouponId { get; private set; }

    /// <summary>User who used the coupon.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Order where coupon was applied.</summary>
    public int OrderId { get; private set; }

    /// <summary>Discount amount applied.</summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>Timestamp of usage.</summary>
    public DateTime UsedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    /// <summary>The coupon.</summary>
    public Coupon Coupon { get; private set; } = null!;

    private CouponUsage() { }

    /// <summary>Records a coupon usage.</summary>
    public static CouponUsage Create(int couponId, string userId, int orderId, decimal discountAmount)
    {
        return new CouponUsage
        {
            CouponId = couponId,
            UserId = userId,
            OrderId = orderId,
            DiscountAmount = discountAmount
        };
    }
}

