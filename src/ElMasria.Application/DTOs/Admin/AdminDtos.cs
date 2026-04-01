using ElMasria.Application.Common;

namespace ElMasria.Application.DTOs.Admin;

/// <summary>High-level dashboard KPIs.</summary>
public sealed record DashboardMetricsDto
{
    public int TotalUsers { get; init; }
    public int TotalOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public int ActiveProducts { get; init; }
    public int PendingOrders { get; init; }
    // Could add more like average order value, conversion rate, etc.
}

/// <summary>Audit log response transfer object.</summary>
public sealed record AuditLogDto
{
    public int Id { get; init; }
    public string? UserEmail { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public string? IpAddress { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>User management projection.</summary>
public sealed record UserManagementDto
{
    public string Id { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}
