using GalleryBetak.Domain.Entities;

namespace GalleryBetak.Domain.Interfaces;

/// <summary>
/// Category specific repository interface adding eager loading for hierarchies.
/// </summary>
public interface ICategoryRepository : IGenericRepository<Category>
{
    /// <summary>
    /// Gets a category by ID and eager-loads its children subcategories.
    /// </summary>
    Task<Category?> GetByIdWithSubcategoriesAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all categories with their children subcategories eager-loaded.
    /// </summary>
    Task<IReadOnlyList<Category>> GetAllWithSubcategoriesAsync(CancellationToken cancellationToken = default);
}

