namespace GalleryBetak.Domain.Interfaces;

/// <summary>
/// Product-specific repository with domain-optimized query methods.
/// </summary>
public interface IProductRepository : IGenericRepository<Entities.Product>
{
    /// <summary>Gets a product by slug with images and category.</summary>
    Task<Entities.Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Gets a product by SKU.</summary>
    Task<Entities.Product?> GetBySKUAsync(string sku, CancellationToken cancellationToken = default);

    /// <summary>Gets featured products.</summary>
    Task<IReadOnlyList<Entities.Product>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>Gets products by category with pagination.</summary>
    Task<(IReadOnlyList<Entities.Product> Items, int TotalCount)> GetByCategoryAsync(
        int categoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Searches products by name (bilingual) with full-text search.</summary>
    Task<(IReadOnlyList<Entities.Product> Items, int TotalCount)> SearchAsync(
        string query, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Gets products with low stock (below threshold).</summary>
    Task<IReadOnlyList<Entities.Product>> GetLowStockAsync(int threshold = 5, CancellationToken cancellationToken = default);

    /// <summary>Gets a product with all includes (images, tags, reviews) for detail page.</summary>
    Task<Entities.Product?> GetDetailAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Order-specific repository with domain-optimized query methods.
/// </summary>
public interface IOrderRepository : IGenericRepository<Entities.Order>
{
    /// <summary>Gets an order by order number.</summary>
    Task<Entities.Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>Gets orders for a specific user with pagination.</summary>
    Task<(IReadOnlyList<Entities.Order> Items, int TotalCount)> GetByUserAsync(
        string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Gets orders by status for admin dashboard.</summary>
    Task<(IReadOnlyList<Entities.Order> Items, int TotalCount)> GetByStatusAsync(
        Enums.OrderStatus status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Gets full order details with items and payments.</summary>
    Task<Entities.Order?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Generates the next order number (ORD-YYYYMM-NNNNN).</summary>
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default);
}

