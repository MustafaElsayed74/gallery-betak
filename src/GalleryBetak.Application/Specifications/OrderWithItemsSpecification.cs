using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Specifications;

namespace GalleryBetak.Application.Specifications;

/// <summary>
/// Advanced Specification to eager-load order items.
/// </summary>
public sealed class OrderWithItemsSpecification : BaseSpecification<Order>
{
    /// <summary>Load a specific user's order by ID.</summary>
    public OrderWithItemsSpecification(int id, string userId) 
        : base(o => o.Id == id && o.UserId == userId)
    {
        AddInclude(o => o.Items);
    }

    /// <summary>Load an order by ID (Admin override).</summary>
    public OrderWithItemsSpecification(int id) 
        : base(o => o.Id == id)
    {
        AddInclude(o => o.Items);
    }

    /// <summary>Load all orders for a specific user.</summary>
    public OrderWithItemsSpecification(string userId) 
        : base(o => o.UserId == userId)
    {
        AddInclude(o => o.Items);
        ApplyOrderByDescending(o => o.CreatedAt);
    }
}

