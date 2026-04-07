using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Product;

namespace GalleryBetak.Application.Interfaces;

/// <summary>
/// Product service contract. Covers catalog browsing, admin CRUD, and search.
/// </summary>
public interface IProductService
{
    /// <summary>Gets a paginated, filtered list of products.</summary>
    Task<ApiResponse<PagedResult<ProductListDto>>> GetProductsAsync(ProductQueryParams query, CancellationToken ct = default);

    /// <summary>Gets a product by ID with full details.</summary>
    Task<ApiResponse<ProductDetailDto>> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Gets a product by slug with full details.</summary>
    Task<ApiResponse<ProductDetailDto>> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Gets featured products for homepage.</summary>
    Task<ApiResponse<IReadOnlyList<ProductListDto>>> GetFeaturedAsync(int count = 8, CancellationToken ct = default);

    /// <summary>Creates a new product (admin).</summary>
    Task<ApiResponse<ProductDetailDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default);

    /// <summary>Updates a product (admin).</summary>
    Task<ApiResponse<ProductDetailDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);

    /// <summary>Soft-deletes a product (admin).</summary>
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Gets low-stock products for admin alerts.</summary>
    Task<ApiResponse<IReadOnlyList<ProductListDto>>> GetLowStockAsync(int threshold = 5, CancellationToken ct = default);
}

