using System.Linq.Expressions;

namespace ElMasria.Domain.Interfaces;

/// <summary>
/// Generic repository interface. Defines common data access operations
/// implemented in Infrastructure layer via EF Core.
/// </summary>
/// <typeparam name="T">Entity type inheriting from BaseEntity.</typeparam>
public interface IGenericRepository<T> where T : Entities.BaseEntity
{
    /// <summary>Gets entity by its primary key.</summary>
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Gets all entities (use sparingly — prefer filtered/paged queries).</summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Finds entities matching a predicate.</summary>
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Gets a single entity matching a predicate.</summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Checks if any entity matches the predicate.</summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Counts entities matching the predicate.</summary>
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Gets a single entity matching the specification.</summary>
    Task<T?> GetEntityWithSpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    /// <summary>Gets a list of entities matching the specification.</summary>
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    /// <summary>Counts entities matching the specification.</summary>
    Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    /// <summary>Adds a new entity.</summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Adds multiple entities.</summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing entity.</summary>
    void Update(T entity);

    /// <summary>Removes an entity.</summary>
    void Remove(T entity);

    /// <summary>Removes multiple entities.</summary>
    void RemoveRange(IEnumerable<T> entities);
}

/// <summary>
/// Unit of Work pattern. Coordinates multiple repository operations
/// within a single database transaction.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>Product repository.</summary>
    IProductRepository Products { get; }

    /// <summary>Cart repository.</summary>
    IGenericRepository<Entities.Cart> Carts { get; }

    /// <summary>Wishlist repository.</summary>
    IGenericRepository<Entities.Wishlist> Wishlists { get; }

    /// <summary>Addresses repository.</summary>
    IGenericRepository<Entities.Address> Addresses { get; }

    /// <summary>Category repository.</summary>
    ICategoryRepository Categories { get; }

    /// <summary>Order repository.</summary>
    IOrderRepository Orders { get; }

    /// <summary>Tag repository.</summary>
    IGenericRepository<Entities.Tag> Tags { get; }

    /// <summary>Coupon repository.</summary>
    IGenericRepository<Entities.Coupon> Coupons { get; }

    /// <summary>Review repository.</summary>
    IGenericRepository<Entities.Review> Reviews { get; }

    /// <summary>Commits all pending changes to the database.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Begins a database transaction.</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Commits the current transaction.</summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back the current transaction.</summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
