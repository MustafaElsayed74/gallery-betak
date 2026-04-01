using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Category;

namespace ElMasria.Application.Interfaces;

/// <summary>
/// Category service contract handling hierarchical categories and breadcrumbs.
/// </summary>
public interface ICategoryService
{
    /// <summary>Gets all root categories with their nested subcategories.</summary>
    Task<ApiResponse<IReadOnlyList<CategoryDto>>> GetHierarchyAsync(CancellationToken ct = default);

    /// <summary>Gets a detailed category including breadcrumb path.</summary>
    Task<ApiResponse<CategoryDetailDto>> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Gets a detailed category by its URL slug.</summary>
    Task<ApiResponse<CategoryDetailDto>> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Creates a new category (admin).</summary>
    Task<ApiResponse<CategoryDetailDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);

    /// <summary>Updates an existing category (admin).</summary>
    Task<ApiResponse<CategoryDetailDto>> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default);

    /// <summary>Deletes a category and its subcategories (admin).</summary>
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default);
}
