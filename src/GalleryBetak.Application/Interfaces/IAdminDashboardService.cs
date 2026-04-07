using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Admin;
using GalleryBetak.Application.DTOs.Order;
using GalleryBetak.Domain.Enums;

namespace GalleryBetak.Application.Interfaces;

/// <summary>
/// Core administrative functions for storefront telemetry and management.
/// </summary>
public interface IAdminDashboardService
{
    /// <summary>Retrieves top-level KPIs.</summary>
    Task<ApiResponse<DashboardMetricsDto>> GetDashboardMetricsAsync(CancellationToken ct = default);

    /// <summary>Retrieves detailed analytics for a configurable period.</summary>
    Task<ApiResponse<DetailedAnalyticsDto>> GetDetailedAnalyticsAsync(int periodDays = 30, CancellationToken ct = default);

    /// <summary>Retrieves recent system audit logs.</summary>
    Task<ApiResponse<IReadOnlyList<AuditLogDto>>> GetRecentAuditLogsAsync(int count = 50, CancellationToken ct = default);

    /// <summary>Lists users with their assigned roles.</summary>
    Task<ApiResponse<PagedResult<UserManagementDto>>> ListUsersAsync(int pageNumber = 1, int pageSize = 20, string? search = null, CancellationToken ct = default);

    /// <summary>Creates a user account by admin.</summary>
    Task<ApiResponse<UserManagementDto>> CreateUserAsync(CreateAdminUserRequest request, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default);

    /// <summary>Updates user profile, status, and optional roles.</summary>
    Task<ApiResponse<UserManagementDto>> UpdateUserAsync(string adminUserId, string targetUserId, UpdateAdminUserRequest request, string ipAddress, string userAgent, CancellationToken ct = default);

    /// <summary>Toggles active/inactive state for a user.</summary>
    Task<ApiResponse<bool>> SetUserStatusAsync(string adminUserId, string targetUserId, bool isActive, string ipAddress, string userAgent, CancellationToken ct = default);

    /// <summary>Soft deletes a user account.</summary>
    Task<ApiResponse<bool>> DeleteUserAsync(string adminUserId, string targetUserId, string ipAddress, string userAgent, CancellationToken ct = default);

    /// <summary>Assigns a role to a user.</summary>
    Task<ApiResponse<bool>> AssignRoleAsync(string adminUserId, string targetUserId, string role, string ipAddress, string userAgent, CancellationToken ct = default);

    /// <summary>Lists coupons with paging and optional search/activation filter.</summary>
    Task<ApiResponse<PagedResult<CouponAdminDto>>> ListCouponsAsync(int pageNumber = 1, int pageSize = 20, string? search = null, bool? isActive = null, CancellationToken ct = default);

    /// <summary>Creates a coupon.</summary>
    Task<ApiResponse<CouponAdminDto>> CreateCouponAsync(CreateCouponRequest request, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default);

    /// <summary>Updates an existing coupon.</summary>
    Task<ApiResponse<CouponAdminDto>> UpdateCouponAsync(int couponId, UpdateCouponRequest request, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default);

    /// <summary>Toggles coupon active state.</summary>
    Task<ApiResponse<bool>> SetCouponStatusAsync(int couponId, bool isActive, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default);

    /// <summary>Soft deletes a coupon.</summary>
    Task<ApiResponse<bool>> DeleteCouponAsync(int couponId, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default);

    /// <summary>Lists discounted products (active offers).</summary>
    Task<ApiResponse<PagedResult<OfferAdminDto>>> ListDiscountOffersAsync(int pageNumber = 1, int pageSize = 20, string? search = null, bool? activeOnly = null, CancellationToken ct = default);

    /// <summary>Applies or updates a product discount offer.</summary>
    Task<ApiResponse<OfferAdminDto>> ApplyDiscountToProductAsync(int productId, ApplyProductDiscountRequest request, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default);

    /// <summary>Removes discount offer from a product.</summary>
    Task<ApiResponse<bool>> ClearProductDiscountAsync(int productId, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default);
    
    /// <summary>Lists orders with optional status filter (admin view).</summary>
    Task<ApiResponse<PagedResult<OrderSummaryDto>>> ListOrdersAsync(int pageNumber = 1, int pageSize = 20, OrderStatus? status = null, CancellationToken ct = default);

    /// <summary>Gets recent orders for the dashboard view.</summary>
    Task<ApiResponse<IReadOnlyList<OrderSummaryDto>>> GetRecentOrdersAsync(int count = 10, CancellationToken ct = default);

    /// <summary>Lists customer service messages for admin inbox.</summary>
    Task<ApiResponse<PagedResult<CustomerServiceMessageDto>>> ListCustomerServiceMessagesAsync(int pageNumber = 1, int pageSize = 20, CustomerServiceMessageStatus? status = null, string? search = null, CancellationToken ct = default);

    /// <summary>Submits a new customer service message from storefront.</summary>
    Task<ApiResponse<CustomerServiceMessageDto>> CreateCustomerServiceMessageAsync(CreateCustomerServiceMessageRequest request, CancellationToken ct = default);

    /// <summary>Updates status/notes for customer service message.</summary>
    Task<ApiResponse<CustomerServiceMessageDto>> UpdateCustomerServiceMessageAsync(string adminUserId, int messageId, UpdateCustomerServiceMessageRequest request, string ipAddress, string userAgent, CancellationToken ct = default);
}

