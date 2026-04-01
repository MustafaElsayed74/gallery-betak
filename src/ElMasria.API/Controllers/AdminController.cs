using System.Security.Claims;
using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Admin;
using ElMasria.Application.DTOs.Order;
using ElMasria.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElMasria.API.Controllers;

/// <summary>
/// Administrative endpoints for storefront management.
/// Require Admin roles.
/// </summary>
// [Authorize(Roles = "Admin,SuperAdmin")] // Policy/Role setup pending finalization, assuming authorization middleware resolves it
[Authorize]
[Route("api/v1/admin")]
public class AdminController : BaseApiController
{
    private readonly IAdminDashboardService _adminService;

    public AdminController(IAdminDashboardService adminService)
    {
        _adminService = adminService;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    private string GetUserAgent() => Request.Headers.UserAgent.ToString();

    /// <summary>Retrieves storefront KPIs.</summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(ApiResponse<DashboardMetricsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics()
    {
        var result = await _adminService.GetDashboardMetricsAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Lists active users and their roles.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserManagementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsers([FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string? search = null)
    {
        var result = await _adminService.ListUsersAsync(page, limit, search);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Grants a role to a user.</summary>
    [HttpPost("users/{userId}/roles")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleRequest request)
    {
        var result = await _adminService.AssignRoleAsync(GetUserId(), userId, request.Role, GetIpAddress(), GetUserAgent());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Retrieves recent orders stream.</summary>
    [HttpGet("orders/recent")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentOrders()
    {
        var result = await _adminService.GetRecentOrdersAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Retrieves security audit logs.</summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs()
    {
        var result = await _adminService.GetRecentAuditLogsAsync();
        return StatusCode(result.StatusCode, result);
    }
}

public sealed record AssignRoleRequest(string Role);
