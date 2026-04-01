using ElMasria.Domain.Entities;
using ElMasria.Domain.Enums;
using ElMasria.Domain.Specifications;

namespace ElMasria.Application.Specifications;

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
