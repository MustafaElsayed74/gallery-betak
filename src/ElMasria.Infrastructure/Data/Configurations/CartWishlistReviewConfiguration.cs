using ElMasria.Domain.Entities;
using ElMasria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElMasria.Infrastructure.Data.Configurations;

/// <summary>EF Core configuration for Cart entity.</summary>
public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).HasMaxLength(450);
        builder.Property(c => c.SessionId).HasMaxLength(100);

        builder.HasIndex(c => c.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        builder.HasIndex(c => c.SessionId)
            .IsUnique()
            .HasFilter("[SessionId] IS NOT NULL");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Coupon)
            .WithMany()
            .HasForeignKey(c => c.CouponId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>EF Core configuration for CartItem entity.</summary>
public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.UnitPrice)
            .HasColumnType("DECIMAL(18,2)");

        builder.HasIndex(ci => ci.CartId);
        builder.HasIndex(ci => ci.ProductId);
        builder.HasIndex(ci => new { ci.CartId, ci.ProductId }).IsUnique();

        builder.HasOne(ci => ci.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ci => ci.Product)
            .WithMany()
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>EF Core configuration for Wishlist entity.</summary>
public sealed class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("Wishlists");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId).HasMaxLength(450).IsRequired();

        builder.HasIndex(w => w.UserId).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>EF Core configuration for WishlistItem entity.</summary>
public sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("WishlistItems");

        builder.HasKey(wi => wi.Id);

        builder.HasIndex(wi => wi.WishlistId);
        builder.HasIndex(wi => wi.ProductId);
        builder.HasIndex(wi => new { wi.WishlistId, wi.ProductId }).IsUnique();

        builder.HasOne(wi => wi.Wishlist)
            .WithMany(w => w.Items)
            .HasForeignKey(wi => wi.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wi => wi.Product)
            .WithMany()
            .HasForeignKey(wi => wi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>EF Core configuration for Address entity.</summary>
public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId).HasMaxLength(450).IsRequired();
        builder.Property(a => a.Label).HasMaxLength(100).IsRequired();
        builder.Property(a => a.RecipientName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Phone).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Governorate).HasMaxLength(100).IsRequired();
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.District).HasMaxLength(100);
        builder.Property(a => a.StreetAddress).HasMaxLength(300).IsRequired();
        builder.Property(a => a.BuildingNo).HasMaxLength(50);
        builder.Property(a => a.ApartmentNo).HasMaxLength(50);
        builder.Property(a => a.PostalCode).HasMaxLength(10);
        builder.Property(a => a.CreatedBy).HasMaxLength(450);

        builder.HasIndex(a => a.UserId)
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

/// <summary>EF Core configuration for Review entity.</summary>
public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId).HasMaxLength(450).IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(2000);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ReviewStatus.Pending);

        builder.Property(r => r.CreatedBy).HasMaxLength(450);

        builder.HasIndex(r => new { r.ProductId, r.Status })
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(r => r.UserId)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(r => new { r.UserId, r.ProductId }).IsUnique();

        builder.HasIndex(r => r.Status)
            .HasFilter("[IsDeleted] = 0 AND [Status] = 'Pending'");

        builder.HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

/// <summary>EF Core configuration for ReviewImage entity.</summary>
public sealed class ReviewImageConfiguration : IEntityTypeConfiguration<ReviewImage>
{
    public void Configure(EntityTypeBuilder<ReviewImage> builder)
    {
        builder.ToTable("ReviewImages");

        builder.HasKey(ri => ri.Id);

        builder.Property(ri => ri.ImageUrl).HasMaxLength(500).IsRequired();

        builder.HasIndex(ri => ri.ReviewId);

        builder.HasOne(ri => ri.Review)
            .WithMany(r => r.Images)
            .HasForeignKey(ri => ri.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
