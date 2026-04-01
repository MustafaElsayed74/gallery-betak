using ElMasria.Domain.Entities;
using ElMasria.Domain.Events;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ElMasria.Infrastructure.Data;

/// <summary>
/// Application database context. Extends IdentityDbContext for ASP.NET Identity integration.
/// Implements global soft-delete filter, automatic audit timestamps, and domain event dispatch.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    /// <summary>Initializes AppDbContext.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Catalog ───────────────────────────────────────────────────
    /// <summary>Categories.</summary>
    public DbSet<Category> Categories => Set<Category>();
    /// <summary>Products.</summary>
    public DbSet<Product> Products => Set<Product>();
    /// <summary>Product images.</summary>
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    /// <summary>Tags.</summary>
    public DbSet<Tag> Tags => Set<Tag>();
    /// <summary>Product-Tag junction.</summary>
    public DbSet<ProductTag> ProductTags => Set<ProductTag>();

    // ── Commerce ──────────────────────────────────────────────────
    /// <summary>Shopping carts.</summary>
    public DbSet<Cart> Carts => Set<Cart>();
    /// <summary>Cart line items.</summary>
    public DbSet<CartItem> CartItems => Set<CartItem>();
    /// <summary>Wishlists.</summary>
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    /// <summary>Wishlist items.</summary>
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    // ── Orders ────────────────────────────────────────────────────
    /// <summary>Orders.</summary>
    public DbSet<Order> Orders => Set<Order>();
    /// <summary>Order line items.</summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    /// <summary>Payments.</summary>
    public DbSet<Payment> Payments => Set<Payment>();

    // ── Coupons ───────────────────────────────────────────────────
    /// <summary>Coupons.</summary>
    public DbSet<Coupon> Coupons => Set<Coupon>();
    /// <summary>Coupon usage tracking.</summary>
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();

    // ── Reviews ───────────────────────────────────────────────────
    /// <summary>Reviews.</summary>
    public DbSet<Review> Reviews => Set<Review>();
    /// <summary>Review images.</summary>
    public DbSet<ReviewImage> ReviewImages => Set<ReviewImage>();

    // ── Addresses ─────────────────────────────────────────────────
    /// <summary>Delivery addresses.</summary>
    public DbSet<Address> Addresses => Set<Address>();

    // ── System ────────────────────────────────────────────────────
    /// <summary>Notifications.</summary>
    public DbSet<Notification> Notifications => Set<Notification>();
    /// <summary>Audit logs.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    /// <summary>Search logs.</summary>
    public DbSet<SearchLog> SearchLogs => Set<SearchLog>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all IEntityTypeConfiguration from this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <inheritdoc/>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-set audit timestamps
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedAt(DateTime.UtcNow);
                    break;
                case EntityState.Modified:
                    entry.Entity.SetUpdatedAt(DateTime.UtcNow);
                    break;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch domain events after successful save
        await DispatchDomainEventsAsync();

        return result;
    }

    private Task DispatchDomainEventsAsync()
    {
        // Collect events from Product entities
        var productEvents = ChangeTracker.Entries<Product>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var entry in ChangeTracker.Entries<Product>())
            entry.Entity.ClearDomainEvents();

        // Collect events from Order entities
        var orderEvents = ChangeTracker.Entries<Order>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var entry in ChangeTracker.Entries<Order>())
            entry.Entity.ClearDomainEvents();

        // Collect events from Payment entities
        var paymentEvents = ChangeTracker.Entries<Payment>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var entry in ChangeTracker.Entries<Payment>())
            entry.Entity.ClearDomainEvents();

        // Domain event handlers will be registered via MediatR in a future phase
        // For now, events are collected and cleared to prevent re-dispatching

        return Task.CompletedTask;
    }
}
