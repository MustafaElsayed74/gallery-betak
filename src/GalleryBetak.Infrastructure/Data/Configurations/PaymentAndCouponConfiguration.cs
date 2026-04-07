using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GalleryBetak.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Payment entity.
/// </summary>
public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);
        builder.Ignore(p => p.DomainEvents);

        builder.Property(p => p.TransactionId).HasMaxLength(200);
        builder.Property(p => p.GatewayOrderId).HasMaxLength(200);

        builder.Property(p => p.Amount)
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("EGP");

        builder.Property(p => p.Method)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentStatus.Pending);

        builder.Property(p => p.GatewayResponse)
            .HasColumnType("NVARCHAR(MAX)");

        builder.Property(p => p.FailureReason).HasMaxLength(500);

        builder.Property(p => p.RefundAmount)
            .HasColumnType("DECIMAL(18,2)");

        builder.HasIndex(p => p.OrderId);
        builder.HasIndex(p => p.TransactionId)
            .IsUnique()
            .HasFilter("[TransactionId] IS NOT NULL");
        builder.HasIndex(p => p.GatewayOrderId)
            .HasFilter("[GatewayOrderId] IS NOT NULL");
        builder.HasIndex(p => p.Status);

        builder.HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// EF Core configuration for Coupon entity.
/// </summary>
public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.Property(c => c.DescriptionAr).HasMaxLength(300);
        builder.Property(c => c.DescriptionEn).HasMaxLength(300);

        builder.Property(c => c.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.DiscountValue)
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(c => c.MinOrderAmount)
            .HasColumnType("DECIMAL(18,2)")
            .HasDefaultValue(0m);

        builder.Property(c => c.MaxDiscountAmount)
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(c => c.CreatedBy).HasMaxLength(450);

        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(c => new { c.IsActive, c.StartsAt, c.ExpiresAt })
            .HasFilter("[IsDeleted] = 0");

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

/// <summary>
/// EF Core configuration for CouponUsage entity.
/// </summary>
public sealed class CouponUsageConfiguration : IEntityTypeConfiguration<CouponUsage>
{
    public void Configure(EntityTypeBuilder<CouponUsage> builder)
    {
        builder.ToTable("CouponUsages");

        builder.HasKey(cu => cu.Id);

        builder.Property(cu => cu.UserId).HasMaxLength(450).IsRequired();

        builder.Property(cu => cu.DiscountAmount)
            .HasColumnType("DECIMAL(18,2)");

        builder.HasIndex(cu => cu.CouponId);
        builder.HasIndex(cu => cu.UserId);
        builder.HasIndex(cu => cu.OrderId);
        builder.HasIndex(cu => new { cu.CouponId, cu.UserId }).IsUnique();

        builder.HasOne(cu => cu.Coupon)
            .WithMany(c => c.Usages)
            .HasForeignKey(cu => cu.CouponId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

