using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Product;

namespace GalleryBetak.Application.Interfaces;

/// <summary>
/// Imports product previews from external storefront URLs for admin review.
/// </summary>
public interface IProductImportService
{
    /// <summary>
    /// Imports and normalizes product preview data from an external URL.
    /// </summary>
    Task<ApiResponse<ProductImportPreviewDto>> ImportFromUrlAsync(ProductImportRequest request, CancellationToken ct = default);
}
