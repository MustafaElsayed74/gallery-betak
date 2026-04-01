using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Admin;
using ElMasria.Application.DTOs.Order;

namespace ElMasria.Application.Interfaces;

/// <summary>
/// Core administrative functions for storefront telemetry and management.
/// </summary>
public interface IAdminDashboardService
{
    /// <summary>Retrieves top-level KPIs.</summary>
    Task<ApiResponse<DashboardMetricsDto>> GetDashboardMetricsAsync(CancellationToken ct = default);

    /// <summary>Retrieves recent system audit logs.</summary>
    Task<ApiResponse<IReadOnlyList<AuditLogDto>>> GetRecentAuditLogsAsync(int count = 50, CancellationToken ct = default);

    /// <summary>Lists users with their assigned roles.</summary>
    Task<ApiResponse<PagedResult<UserManagementDto>>> ListUsersAsync(int pageNumber = 1, int pageSize = 20, string? search = null, CancellationToken ct = default);

    /// <summary>Assigns a role to a user.</summary>
    Task<ApiResponse<bool>> AssignRoleAsync(string adminUserId, string targetUserId, string role, string ipAddress, string userAgent, CancellationToken ct = default);
    
    /// <summary>Gets recent orders for the dashboard view.</summary>
    Task<ApiResponse<IReadOnlyList<OrderSummaryDto>>> GetRecentOrdersAsync(int count = 10, CancellationToken ct = default);
}
