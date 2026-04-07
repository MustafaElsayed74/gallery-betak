using System.Security.Claims;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Admin;
using GalleryBetak.Application.DTOs.Order;
using GalleryBetak.Application.DTOs.Product;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GalleryBetak.API.Controllers;

/// <summary>
/// Administrative endpoints for storefront management.
/// Require Admin roles.
/// </summary>
[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin")]
public class AdminController : BaseApiController
{
    private readonly IAdminDashboardService _adminService;
    private readonly IOrderService _orderService;
    private readonly IProductImportService _productImportService;

    public AdminController(
        IAdminDashboardService adminService,
        IOrderService orderService,
        IProductImportService productImportService)
    {
        _adminService = adminService;
        _orderService = orderService;
        _productImportService = productImportService;
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

    /// <summary>Retrieves detailed analytics for a configurable period.</summary>
    [HttpGet("analytics/detailed")]
    [ProducesResponseType(typeof(ApiResponse<DetailedAnalyticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetailedAnalytics([FromQuery] int days = 30)
    {
        var result = await _adminService.GetDetailedAnalyticsAsync(days);
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

    /// <summary>Creates a user account from admin console.</summary>
    [HttpPost("users")]
    [ProducesResponseType(typeof(ApiResponse<UserManagementDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserRequest request)
    {
        var result = await _adminService.CreateUserAsync(request, GetUserId(), GetIpAddress(), GetUserAgent());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Updates user profile and optional roles.</summary>
    [HttpPut("users/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<UserManagementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateAdminUserRequest request)
    {
        var result = await _adminService.UpdateUserAsync(GetUserId(), userId, request, GetIpAddress(), GetUserAgent());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Activates/deactivates a user account.</summary>
    [HttpPatch("users/{userId}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetUserStatus(string userId, [FromBody] SetUserStatusRequest request)
    {
        var result = await _adminService.SetUserStatusAsync(GetUserId(), userId, request.IsActive, GetIpAddress(), GetUserAgent());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Soft deletes a user account.</summary>
    [HttpDelete("users/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var result = await _adminService.DeleteUserAsync(GetUserId(), userId, GetIpAddress(), GetUserAgent());
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

    /// <summary>Lists coupons with paging and optional filtering.</summary>
    [HttpGet("coupons")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CouponAdminDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCoupons(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _adminService.ListCouponsAsync(page, limit, search, isActive);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Creates a coupon for storefront promotions.</summary>
    [HttpPost("coupons")]
    [ProducesResponseType(typeof(ApiResponse<CouponAdminDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponRequest request)
    {
        var result = await _adminService.CreateCouponAsync(request, GetUserId(), GetIpAddress(), GetUserAgent());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Updates coupon settings and validity.</summary>
    [HttpPut("coupons/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CouponAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCoupon(int id, [FromBody] UpdateCouponRequest request)
    {
        var result = await _adminService.UpdateCouponAsync(id, request, GetUserId(), GetIpAddress(), GetUserAgent());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Activates/deactivates a coupon.</summary>
    [HttpPatch("coupons/{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetCouponStatus(int id, [FromBody] SetCouponStatusRequest request)
    {
        var result = await _adminService.SetCouponStatusAsync(id, request.IsActive, GetUserId(), GetIpAddress(), GetUserAgent());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Soft deletes a coupon.</summary>
    [HttpDelete("coupons/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCoupon(int id)
    {
        var result = await _adminService.DeleteCouponAsync(id, GetUserId(), GetIpAddress(), GetUserAgent());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Lists orders with optional status filter and paging.</summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOrders([FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] OrderStatus? status = null)
    {
        var result = await _adminService.ListOrdersAsync(page, limit, status);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Updates order lifecycle status.</summary>
    [HttpPatch("orders/{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, request.Status, request.TrackingNumber, request.Reason);
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

    /// <summary>Lists active product discount offers.</summary>
    [HttpGet("offers")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OfferAdminDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOffers(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? activeOnly = null)
    {
        var result = await _adminService.ListDiscountOffersAsync(page, limit, search, activeOnly);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Applies or updates product discount offer.</summary>
    [HttpPatch("offers/{productId:int}/discount")]
    [ProducesResponseType(typeof(ApiResponse<OfferAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyOfferDiscount(int productId, [FromBody] ApplyProductDiscountRequest request)
    {
        var result = await _adminService.ApplyDiscountToProductAsync(productId, request, GetUserId(), GetIpAddress(), GetUserAgent());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Removes discount offer from a product.</summary>
    [HttpDelete("offers/{productId:int}/discount")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearOfferDiscount(int productId)
    {
        var result = await _adminService.ClearProductDiscountAsync(productId, GetUserId(), GetIpAddress(), GetUserAgent());
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

    /// <summary>Lists support messages sent by customers.</summary>
    [HttpGet("support/messages")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CustomerServiceMessageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSupportMessages(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] CustomerServiceMessageStatus? status = null,
        [FromQuery] string? search = null)
    {
        var result = await _adminService.ListCustomerServiceMessagesAsync(page, limit, status, search);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Updates support message status and admin notes.</summary>
    [HttpPatch("support/messages/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerServiceMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSupportMessage(int id, [FromBody] UpdateCustomerServiceMessageRequest request)
    {
        var result = await _adminService.UpdateCustomerServiceMessageAsync(GetUserId(), id, request, GetIpAddress(), GetUserAgent());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Imports product preview data from an external URL for admin review.</summary>
    [HttpPost("products/import")]
    [ProducesResponseType(typeof(ApiResponse<ProductImportPreviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ImportProduct([FromBody] ProductImportRequest request)
    {
        var result = await _productImportService.ImportFromUrlAsync(request);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed record AssignRoleRequest(string Role);
public sealed record UpdateOrderStatusRequest(OrderStatus Status, string? TrackingNumber, string? Reason);

