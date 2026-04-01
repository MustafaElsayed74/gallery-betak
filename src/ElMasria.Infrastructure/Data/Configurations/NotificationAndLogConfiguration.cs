using ElMasria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElMasria.Infrastructure.Data.Configurations;

/// <summary>EF Core configuration for Notification entity.</summary>
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId).HasMaxLength(450).IsRequired();
        builder.Property(n => n.TitleAr).HasMaxLength(300).IsRequired();
        builder.Property(n => n.TitleEn).HasMaxLength(300).IsRequired();
        builder.Property(n => n.MessageAr).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.MessageEn).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
        builder.Property(n => n.ReferenceType).HasMaxLength(50);

        builder.HasIndex(n => new { n.UserId, n.CreatedAt })
            .IsDescending(false, true);

        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasFilter("[IsRead] = 0");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>EF Core configuration for AuditLog entity (immutable).</summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId).HasMaxLength(450);
        builder.Property(a => a.UserEmail).HasMaxLength(256);
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(50);
        builder.Property(a => a.OldValues).HasColumnType("NVARCHAR(MAX)");
        builder.Property(a => a.NewValues).HasColumnType("NVARCHAR(MAX)");
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasMaxLength(500);

        builder.HasIndex(a => new { a.UserId, a.Timestamp })
            .IsDescending(false, true);

        builder.HasIndex(a => new { a.EntityType, a.EntityId });

        builder.HasIndex(a => a.Timestamp)
            .IsDescending();

        builder.HasIndex(a => a.Action);
    }
}

/// <summary>EF Core configuration for SearchLog entity.</summary>
public sealed class SearchLogConfiguration : IEntityTypeConfiguration<SearchLog>
{
    public void Configure(EntityTypeBuilder<SearchLog> builder)
    {
        builder.ToTable("SearchLogs");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Query).HasMaxLength(500).IsRequired();
        builder.Property(s => s.UserId).HasMaxLength(450);
        builder.Property(s => s.Language).HasMaxLength(5).HasDefaultValue("ar");

        builder.HasIndex(s => s.Query);
        builder.HasIndex(s => s.Timestamp).IsDescending();
        builder.HasIndex(s => s.ResultCount)
            .HasFilter("[ResultCount] = 0");
    }
}

/// <summary>EF Core configuration for ApplicationUser (extended Identity user).</summary>
public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.ProfileImageUrl).HasMaxLength(500);
        builder.Property(u => u.RefreshToken).HasMaxLength(500);

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
