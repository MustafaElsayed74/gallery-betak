using ElMasria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElMasria.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for Product entity.
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        // Ignore domain events — not persisted
        builder.Ignore(p => p.DomainEvents);

        builder.Property(p => p.NameAr)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(p => p.NameEn)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(p => p.DescriptionAr)
            .HasColumnType("NVARCHAR(MAX)");

        builder.Property(p => p.DescriptionEn)
            .HasColumnType("NVARCHAR(MAX)");

        builder.Property(p => p.Price)
            .HasColumnType("DECIMAL(18,2)")
            .IsRequired();

        builder.Property(p => p.OriginalPrice)
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(p => p.SKU)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Weight)
            .HasColumnType("DECIMAL(10,3)");

        builder.Property(p => p.Dimensions)
            .HasMaxLength(100);

        builder.Property(p => p.Material)
            .HasMaxLength(200);

        builder.Property(p => p.Origin)
            .HasMaxLength(200);

        builder.Property(p => p.AverageRating)
            .HasColumnType("DECIMAL(3,2)")
            .HasDefaultValue(0.00m);

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(450);

        // Unique constraints
        builder.HasIndex(p => p.SKU)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(p => p.Slug)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Query indexes
        builder.HasIndex(p => p.CategoryId)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(p => new { p.IsFeatured, p.IsActive })
            .HasFilter("[IsDeleted] = 0 AND [IsFeatured] = 1");

        builder.HasIndex(p => p.Price)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(p => p.CreatedAt)
            .IsDescending()
            .HasFilter("[IsDeleted] = 0 AND [IsActive] = 1");

        builder.HasIndex(p => p.AverageRating)
            .IsDescending()
            .HasFilter("[IsDeleted] = 0 AND [IsActive] = 1");

        // Relationship
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global soft-delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

/// <summary>
/// EF Core Fluent API configuration for ProductImage entity.
/// </summary>
public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.ImageUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(pi => pi.ThumbnailUrl)
            .HasMaxLength(500);

        builder.Property(pi => pi.AltTextAr)
            .HasMaxLength(300);

        builder.Property(pi => pi.AltTextEn)
            .HasMaxLength(300);

        builder.HasIndex(pi => new { pi.ProductId, pi.DisplayOrder });

        builder.HasOne(pi => pi.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF Core Fluent API configuration for Tag entity.
/// </summary>
public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.NameAr)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.NameEn)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(t => t.Slug).IsUnique();

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}

/// <summary>
/// EF Core configuration for ProductTag junction table.
/// </summary>
public sealed class ProductTagConfiguration : IEntityTypeConfiguration<ProductTag>
{
    public void Configure(EntityTypeBuilder<ProductTag> builder)
    {
        builder.ToTable("ProductTags");

        builder.HasKey(pt => new { pt.ProductId, pt.TagId });

        builder.HasOne(pt => pt.Product)
            .WithMany(p => p.ProductTags)
            .HasForeignKey(pt => pt.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.Tag)
            .WithMany(t => t.ProductTags)
            .HasForeignKey(pt => pt.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pt => pt.TagId);
    }
}
