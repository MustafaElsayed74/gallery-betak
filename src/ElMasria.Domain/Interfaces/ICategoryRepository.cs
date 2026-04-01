using ElMasria.Domain.Entities;

namespace ElMasria.Domain.Interfaces;

/// <summary>
/// Category specific repository interface adding eager loading for hierarchies.
/// </summary>
public interface ICategoryRepository : IGenericRepository<Category>
{
    /// <summary>
    /// Gets a category by ID and eager-loads its children subcategories.
    /// </summary>
    Task<Category?> GetByIdWithSubcategoriesAsync(int id, CancellationToken cancellationToken = default);
}
