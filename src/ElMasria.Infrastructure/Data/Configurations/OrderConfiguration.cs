using ElMasria.Domain.Entities;
using ElMasria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElMasria.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for Order entity.
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Ignore(o => o.DomainEvents);

        builder.Property(o => o.OrderNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.UserId)
            .HasMaxLength(450);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(OrderStatus.Pending);

        builder.Property(o => o.SubTotal)
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(o => o.ShippingCost)
            .HasColumnType("DECIMAL(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.TaxAmount)
            .HasColumnType("DECIMAL(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.DiscountAmount)
            .HasColumnType("DECIMAL(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.TotalAmount)
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(o => o.CouponCode)
            .HasMaxLength(50);

        builder.Property(o => o.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(o => o.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentStatus.Pending);

        builder.Property(o => o.Notes)
            .HasMaxLength(500);

        // Address snapshot
        builder.Property(o => o.ShippingRecipientName).HasMaxLength(200).IsRequired();
        builder.Property(o => o.ShippingPhone).HasMaxLength(20).IsRequired();
        builder.Property(o => o.ShippingGovernorate).HasMaxLength(100).IsRequired();
        builder.Property(o => o.ShippingCity).HasMaxLength(100).IsRequired();
        builder.Property(o => o.ShippingDistrict).HasMaxLength(100);
        builder.Property(o => o.ShippingStreetAddress).HasMaxLength(300).IsRequired();
        builder.Property(o => o.ShippingBuildingNo).HasMaxLength(50);
        builder.Property(o => o.ShippingApartmentNo).HasMaxLength(50);
        builder.Property(o => o.ShippingPostalCode).HasMaxLength(10);

        builder.Property(o => o.TrackingNumber).HasMaxLength(100);
        builder.Property(o => o.CancellationReason).HasMaxLength(500);
        builder.Property(o => o.CreatedBy).HasMaxLength(450);

        // Indexes
        builder.HasIndex(o => o.OrderNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(o => new { o.UserId, o.CreatedAt })
            .IsDescending(false, true)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(o => new { o.Status, o.CreatedAt })
            .IsDescending(false, true)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(o => o.PaymentStatus)
            .HasFilter("[IsDeleted] = 0");

        // Relationships
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Coupon>()
            .WithMany()
            .HasForeignKey(o => o.CouponId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}

/// <summary>
/// EF Core Fluent API configuration for OrderItem entity.
/// </summary>
public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.ProductNameAr).HasMaxLength(300).IsRequired();
        builder.Property(oi => oi.ProductNameEn).HasMaxLength(300).IsRequired();
        builder.Property(oi => oi.ProductSKU).HasMaxLength(50).IsRequired();
        builder.Property(oi => oi.ProductImageUrl).HasMaxLength(500);

        builder.Property(oi => oi.UnitPrice)
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(oi => oi.TotalPrice)
            .HasColumnType("DECIMAL(18,2)");

        builder.HasIndex(oi => oi.OrderId);
        builder.HasIndex(oi => oi.ProductId);

        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
