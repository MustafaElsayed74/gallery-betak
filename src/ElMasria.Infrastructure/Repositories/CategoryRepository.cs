using ElMasria.Domain.Entities;
using ElMasria.Domain.Interfaces;
using ElMasria.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ElMasria.Infrastructure.Repositories;

/// <summary>
/// Category repository implementation with eager loading.
/// </summary>
public sealed class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Category?> GetByIdWithSubcategoriesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
