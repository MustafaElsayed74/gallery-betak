using ElMasria.Domain.Entities;
using ElMasria.Domain.Interfaces;
using ElMasria.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ElMasria.Infrastructure.Repositories;

/// <summary>
/// Product repository with optimized queries for product catalog operations.
/// </summary>
public sealed class ProductRepository : GenericRepository<Product>, IProductRepository
{
    /// <summary>Initializes ProductRepository.</summary>
    public ProductRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Slug == slug.ToLowerInvariant(), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Product?> GetBySKUAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.SKU == sku.ToUpperInvariant(), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Product>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(p => p.IsFeatured && p.IsActive)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetByCategoryAsync(
        int categoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .Include(p => p.Images.Where(i => i.IsPrimary));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
        string searchQuery, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = searchQuery.Trim().ToLowerInvariant();

        var query = DbSet
            .AsNoTracking()
            .Where(p => p.IsActive &&
                (p.NameAr.Contains(normalizedQuery) ||
                 p.NameEn.Contains(normalizedQuery) ||
                 (p.DescriptionAr != null && p.DescriptionAr.Contains(normalizedQuery)) ||
                 (p.DescriptionEn != null && p.DescriptionEn.Contains(normalizedQuery)) ||
                 p.SKU.Contains(normalizedQuery)))
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Include(p => p.Category);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.ViewCount)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold = 5,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(p => p.IsActive && p.StockQuantity < threshold && p.StockQuantity > 0)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Product?> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Reviews.Where(r => r.Status == Domain.Enums.ReviewStatus.Approved))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
