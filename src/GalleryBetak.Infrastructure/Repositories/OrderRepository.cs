using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Enums;
using GalleryBetak.Domain.Interfaces;
using GalleryBetak.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GalleryBetak.Infrastructure.Repositories;

/// <summary>
/// Order repository with optimized queries for order management.
/// </summary>
public sealed class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    /// <summary>Initializes OrderRepository.</summary>
    public OrderRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetByUserAsync(
        string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Include(o => o.Items);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetByStatusAsync(
        OrderStatus status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(o => o.Status == status)
            .Include(o => o.Items);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<Order?> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        var prefix = $"ORD-{DateTime.UtcNow:yyyyMM}";

        var lastOrder = await DbSet
            .IgnoreQueryFilters() // Include soft-deleted orders for number continuity
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastOrder is null)
            return $"{prefix}-00001";

        var lastSequence = int.Parse(lastOrder[(prefix.Length + 1)..]);
        return $"{prefix}-{(lastSequence + 1):D5}";
    }
}

