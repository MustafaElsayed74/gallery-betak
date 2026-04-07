using GalleryBetak.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GalleryBetak.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for Category entity.
/// </summary>
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.NameAr)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(c => c.NameEn)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(c => c.DescriptionAr)
            .HasColumnType("NVARCHAR(MAX)");

        builder.Property(c => c.DescriptionEn)
            .HasColumnType("NVARCHAR(MAX)");

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(500);

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(450);

        // Indexes
        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(c => c.ParentId)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(c => new { c.IsActive, c.DisplayOrder })
            .HasFilter("[IsDeleted] = 0");

        // Self-referencing relationship
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.NoAction);

        // Global soft-delete filter
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

