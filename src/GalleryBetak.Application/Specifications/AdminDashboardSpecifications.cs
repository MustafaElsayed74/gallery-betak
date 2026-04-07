using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Enums;
using GalleryBetak.Domain.Specifications;

namespace GalleryBetak.Application.Specifications;

/// <summary>Specification to count orders optionally filtered by status.</summary>
public sealed class OrderCountSpecification : BaseSpecification<Order>
{
    public OrderCountSpecification(OrderStatus? status = null) 
        : base(o => !status.HasValue || o.Status == status.Value)
    {
    }
}

/// <summary>Specification to retrieve active products.</summary>
public sealed class ActiveProductCountSpecification : BaseSpecification<Product>
{
    public ActiveProductCountSpecification() 
        : base(p => p.IsActive && !p.IsDeleted)
    {
    }
}

/// <summary>Specification to count products with low stock.</summary>
public sealed class LowStockProductsCountSpecification : BaseSpecification<Product>
{
    public LowStockProductsCountSpecification(int threshold = 5)
        : base(p => p.StockQuantity <= threshold && p.IsActive && !p.IsDeleted)
    {
    }
}

/// <summary>Specification to get recent audit logs.</summary>
public sealed class RecentAuditLogsSpecification : BaseSpecification<AuditLog>
{
    public RecentAuditLogsSpecification(int take) 
        : base(null)
    {
        ApplyOrderByDescending(a => a.Timestamp);
        ApplyPaging(0, take);
    }
}

/// <summary>Specification to get recent orders.</summary>
public sealed class RecentOrdersSpecification : BaseSpecification<Order>
{
    public RecentOrdersSpecification(int take) 
        : base(null)
    {
        AddInclude(o => o.Items);
        ApplyOrderByDescending(o => o.CreatedAt);
        ApplyPaging(0, take);
    }
}

/// <summary>Specification to get orders representing finalized revenue.</summary>
public sealed class SuccessfulOrdersSpecification : BaseSpecification<Order>
{
    public SuccessfulOrdersSpecification() 
        : base(o => o.Status != OrderStatus.Cancelled)
    {
    }
}

/// <summary>Specification to get revenue-eligible orders in the last 30 days.</summary>
public sealed class RevenueLast30DaysOrdersSpecification : BaseSpecification<Order>
{
    public RevenueLast30DaysOrdersSpecification()
        : this(DateTime.UtcNow.AddDays(-30))
    {
    }

    private RevenueLast30DaysOrdersSpecification(DateTime sinceUtc)
        : base(o => o.Status != OrderStatus.Cancelled && o.CreatedAt >= sinceUtc)
    {
    }
}

/// <summary>Specification to list orders for admin with optional status filter.</summary>
public sealed class AdminOrdersSpecification : BaseSpecification<Order>
{
    public AdminOrdersSpecification(int skip, int take, OrderStatus? status = null)
        : base(o => !status.HasValue || o.Status == status.Value)
    {
        AddInclude(o => o.Items);
        ApplyOrderByDescending(o => o.CreatedAt);
        ApplyPaging(skip, take);
    }
}

/// <summary>Specification to count orders for admin with optional status filter.</summary>
public sealed class AdminOrdersCountSpecification : BaseSpecification<Order>
{
    public AdminOrdersCountSpecification(OrderStatus? status = null)
        : base(o => !status.HasValue || o.Status == status.Value)
    {
    }
}

/// <summary>Specification to count open customer service messages.</summary>
public sealed class OpenCustomerServiceMessagesCountSpecification : BaseSpecification<CustomerServiceMessage>
{
    public OpenCustomerServiceMessagesCountSpecification()
        : base(m => m.Status == CustomerServiceMessageStatus.New || m.Status == CustomerServiceMessageStatus.InProgress)
    {
    }
}

/// <summary>Specification to count resolved/closed customer service messages.</summary>
public sealed class ResolvedCustomerServiceMessagesCountSpecification : BaseSpecification<CustomerServiceMessage>
{
    public ResolvedCustomerServiceMessagesCountSpecification()
        : base(m => m.Status == CustomerServiceMessageStatus.Resolved || m.Status == CustomerServiceMessageStatus.Closed)
    {
    }
}

/// <summary>Specification to list customer service messages with filters and paging.</summary>
public sealed class CustomerServiceMessagesSpecification : BaseSpecification<CustomerServiceMessage>
{
    public CustomerServiceMessagesSpecification(int skip, int take, CustomerServiceMessageStatus? status = null, string? search = null)
        : base(m =>
            (!status.HasValue || m.Status == status.Value) &&
            (string.IsNullOrWhiteSpace(search) ||
             m.Name.Contains(search) ||
             m.Email.Contains(search) ||
             m.Subject.Contains(search)))
    {
        ApplyOrderByDescending(m => m.CreatedAt);
        ApplyPaging(skip, take);
    }
}

/// <summary>Specification to count customer service messages with filters.</summary>
public sealed class CustomerServiceMessagesCountSpecification : BaseSpecification<CustomerServiceMessage>
{
    public CustomerServiceMessagesCountSpecification(CustomerServiceMessageStatus? status = null, string? search = null)
        : base(m =>
            (!status.HasValue || m.Status == status.Value) &&
            (string.IsNullOrWhiteSpace(search) ||
             m.Name.Contains(search) ||
             m.Email.Contains(search) ||
             m.Subject.Contains(search)))
    {
    }
}

/// <summary>Specification to list coupons with optional filters and paging.</summary>
public sealed class AdminCouponsSpecification : BaseSpecification<Coupon>
{
    public AdminCouponsSpecification(int skip, int take, string? search = null, bool? isActive = null)
        : base(c =>
            (!isActive.HasValue || c.IsActive == isActive.Value) &&
            (string.IsNullOrWhiteSpace(search) ||
             c.Code.Contains(search) ||
             (c.DescriptionAr != null && c.DescriptionAr.Contains(search)) ||
             (c.DescriptionEn != null && c.DescriptionEn.Contains(search))))
    {
        ApplyOrderByDescending(c => c.CreatedAt);
        ApplyPaging(skip, take);
    }
}

/// <summary>Specification to count coupons with optional filters.</summary>
public sealed class AdminCouponsCountSpecification : BaseSpecification<Coupon>
{
    public AdminCouponsCountSpecification(string? search = null, bool? isActive = null)
        : base(c =>
            (!isActive.HasValue || c.IsActive == isActive.Value) &&
            (string.IsNullOrWhiteSpace(search) ||
             c.Code.Contains(search) ||
             (c.DescriptionAr != null && c.DescriptionAr.Contains(search)) ||
             (c.DescriptionEn != null && c.DescriptionEn.Contains(search))))
    {
    }
}

/// <summary>Specification to list discounted products with optional filters and paging.</summary>
public sealed class DiscountOffersSpecification : BaseSpecification<Product>
{
    public DiscountOffersSpecification(int skip, int take, string? search = null, bool? activeOnly = null)
        : base(p =>
            p.OriginalPrice.HasValue &&
            p.OriginalPrice > p.Price &&
            (!activeOnly.HasValue || !activeOnly.Value || p.IsActive) &&
            (string.IsNullOrWhiteSpace(search) ||
             p.NameAr.Contains(search) ||
             p.NameEn.Contains(search) ||
             p.SKU.Contains(search) ||
             p.Slug.Contains(search)))
    {
        AddInclude(p => p.Category);
        ApplyOrderByDescending(p => p.CreatedAt);
        ApplyPaging(skip, take);
    }
}

/// <summary>Specification to count discounted products with optional filters.</summary>
public sealed class DiscountOffersCountSpecification : BaseSpecification<Product>
{
    public DiscountOffersCountSpecification(string? search = null, bool? activeOnly = null)
        : base(p =>
            p.OriginalPrice.HasValue &&
            p.OriginalPrice > p.Price &&
            (!activeOnly.HasValue || !activeOnly.Value || p.IsActive) &&
            (string.IsNullOrWhiteSpace(search) ||
             p.NameAr.Contains(search) ||
             p.NameEn.Contains(search) ||
             p.SKU.Contains(search) ||
             p.Slug.Contains(search)))
    {
    }
}

/// <summary>Specification to retrieve orders with items inside an analytics date window.</summary>
public sealed class OrdersForAnalyticsSpecification : BaseSpecification<Order>
{
    public OrdersForAnalyticsSpecification(DateTime fromUtcInclusive, DateTime toUtcExclusive)
        : base(o => o.CreatedAt >= fromUtcInclusive && o.CreatedAt < toUtcExclusive)
    {
        AddInclude(o => o.Items);
        ApplyOrderBy(o => o.CreatedAt);
    }
}

