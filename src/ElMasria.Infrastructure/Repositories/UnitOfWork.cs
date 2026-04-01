using ElMasria.Domain.Entities;
using ElMasria.Domain.Interfaces;
using ElMasria.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElMasria.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation. Coordinates multiple repositories
/// within a single database transaction.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    // Lazy-initialized repositories
    private IProductRepository? _products;
    private IOrderRepository? _orders;
    private ICategoryRepository? _categories;
    private IGenericRepository<Cart>? _carts;
    private IGenericRepository<Wishlist>? _wishlists;
    private IGenericRepository<Tag>? _tags;
    private IGenericRepository<Coupon>? _coupons;
    private IGenericRepository<Review>? _reviews;

    /// <summary>Initializes UnitOfWork with the database context.</summary>
    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public IProductRepository Products =>
        _products ??= new ProductRepository(_context);

    /// <inheritdoc/>
    public IOrderRepository Orders =>
        _orders ??= new OrderRepository(_context);

    /// <inheritdoc/>
    public ICategoryRepository Categories =>
        _categories ??= new CategoryRepository(_context);

    /// <inheritdoc/>
    public IGenericRepository<Cart> Carts =>
        _carts ??= new GenericRepository<Cart>(_context);

    /// <inheritdoc/>
    public IGenericRepository<Wishlist> Wishlists =>
        _wishlists ??= new GenericRepository<Wishlist>(_context);

    /// <inheritdoc/>
    public IGenericRepository<Tag> Tags =>
        _tags ??= new GenericRepository<Tag>(_context);

    /// <inheritdoc/>
    public IGenericRepository<Coupon> Coupons =>
        _coupons ??= new GenericRepository<Coupon>(_context);

    /// <inheritdoc/>
    public IGenericRepository<Review> Reviews =>
        _reviews ??= new GenericRepository<Review>(_context);

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc/>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    /// <summary>Disposes the context and transaction.</summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction?.Dispose();
            _context.Dispose();
            _disposed = true;
        }
    }
}
