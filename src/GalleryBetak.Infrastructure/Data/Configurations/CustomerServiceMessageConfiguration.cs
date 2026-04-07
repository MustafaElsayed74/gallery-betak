using GalleryBetak.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GalleryBetak.Infrastructure.Data.Configurations;

/// <summary>EF Core configuration for customer service messages.</summary>
public sealed class CustomerServiceMessageConfiguration : IEntityTypeConfiguration<CustomerServiceMessage>
{
    public void Configure(EntityTypeBuilder<CustomerServiceMessage> builder)
    {
        builder.ToTable("CustomerServiceMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(m => m.Subject)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(m => m.Message)
            .HasColumnType("NVARCHAR(MAX)")
            .IsRequired();

        builder.Property(m => m.AdminNotes)
            .HasColumnType("NVARCHAR(MAX)");

        builder.Property(m => m.HandledByUserId)
            .HasMaxLength(450);

        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.CreatedAt).IsDescending();
        builder.HasIndex(m => new { m.Status, m.CreatedAt }).IsDescending(false, true);

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
