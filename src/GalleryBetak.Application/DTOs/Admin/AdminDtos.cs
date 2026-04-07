using GalleryBetak.Application.Common;
using GalleryBetak.Domain.Enums;

namespace GalleryBetak.Application.DTOs.Admin;

/// <summary>High-level dashboard KPIs.</summary>
public sealed record DashboardMetricsDto
{
    public int TotalUsers { get; init; }
    public int TotalOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public int ActiveProducts { get; init; }
    public int PendingOrders { get; init; }
    public int CancelledOrders { get; init; }
    public int LowStockProducts { get; init; }
    public decimal RevenueLast30Days { get; init; }
    public int OpenSupportMessages { get; init; }
    public int ResolvedSupportMessages { get; init; }
}

/// <summary>Time-series analytics point for dashboard charts.</summary>
public sealed record AnalyticsDailyPointDto
{
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
    public int OrdersCount { get; init; }
    public decimal DiscountAmount { get; init; }
}

/// <summary>Order status distribution in an analytics window.</summary>
public sealed record AnalyticsOrderStatusBreakdownDto
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Revenue { get; init; }
}

/// <summary>Coupon performance aggregate for the selected analytics window.</summary>
public sealed record CouponPerformanceDto
{
    public string CouponCode { get; init; } = string.Empty;
    public int OrdersCount { get; init; }
    public decimal TotalDiscountAmount { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AverageDiscountAmount { get; init; }
}

/// <summary>Top-selling products aggregate for the selected analytics window.</summary>
public sealed record TopProductPerformanceDto
{
    public int ProductId { get; init; }
    public string ProductNameAr { get; init; } = string.Empty;
    public string ProductNameEn { get; init; } = string.Empty;
    public int QuantitySold { get; init; }
    public decimal Revenue { get; init; }
}

/// <summary>Detailed analytics dataset for admin dashboard.</summary>
public sealed record DetailedAnalyticsDto
{
    public int PeriodDays { get; init; }
    public decimal RevenueInPeriod { get; init; }
    public int OrdersInPeriod { get; init; }
    public decimal AverageOrderValue { get; init; }
    public decimal DiscountsInPeriod { get; init; }
    public int CouponOrdersInPeriod { get; init; }
    public decimal CouponRevenueInPeriod { get; init; }
    public IReadOnlyList<AnalyticsDailyPointDto> DailyTrend { get; init; } = [];
    public IReadOnlyList<AnalyticsOrderStatusBreakdownDto> OrderStatusBreakdown { get; init; } = [];
    public IReadOnlyList<CouponPerformanceDto> CouponPerformance { get; init; } = [];
    public IReadOnlyList<TopProductPerformanceDto> TopProducts { get; init; } = [];
}

/// <summary>Coupon projection for admin management.</summary>
public sealed record CouponAdminDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public decimal MinOrderAmount { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public int UsageLimit { get; init; }
    public int UsedCount { get; init; }
    public DateTime StartsAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsActive { get; init; }
    public bool IsValid { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Request payload for creating admin coupons.</summary>
public sealed record CreateCouponRequest
{
    public string Code { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal MinOrderAmount { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public int UsageLimit { get; init; }
    public DateTime StartsAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>Request payload for updating admin coupons.</summary>
public sealed record UpdateCouponRequest
{
    public string Code { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal MinOrderAmount { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public int UsageLimit { get; init; }
    public DateTime StartsAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>Simple coupon status toggle request.</summary>
public sealed record SetCouponStatusRequest(bool IsActive);

/// <summary>Discounted product projection for offer management.</summary>
public sealed record OfferAdminDto
{
    public int ProductId { get; init; }
    public string ProductNameAr { get; init; } = string.Empty;
    public string ProductNameEn { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? OriginalPrice { get; init; }
    public int DiscountPercentage { get; init; }
    public int StockQuantity { get; init; }
    public bool IsActive { get; init; }
    public string? CategoryNameAr { get; init; }
    public string? CategoryNameEn { get; init; }
}

/// <summary>Request payload to apply/adjust a discount on a product.</summary>
public sealed record ApplyProductDiscountRequest
{
    public decimal? DiscountedPrice { get; init; }
    public int? DiscountPercentage { get; init; }
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
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}

/// <summary>Request payload for creating users from admin panel.</summary>
public sealed record CreateAdminUserRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; } = true;
    public IReadOnlyList<string> Roles { get; init; } = [];
}

/// <summary>Request payload for updating users from admin panel.</summary>
public sealed record UpdateAdminUserRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; } = true;
    public IReadOnlyList<string>? Roles { get; init; }
}

/// <summary>Simple user status toggle request.</summary>
public sealed record SetUserStatusRequest(bool IsActive);

/// <summary>Customer service message projected for admin handling.</summary>
public sealed record CustomerServiceMessageDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? AdminNotes { get; init; }
    public string? HandledByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}

/// <summary>Public request to submit customer service message.</summary>
public sealed record CreateCustomerServiceMessageRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

/// <summary>Admin request to update support message status and notes.</summary>
public sealed record UpdateCustomerServiceMessageRequest
{
    public CustomerServiceMessageStatus Status { get; init; }
    public string? AdminNotes { get; init; }
}

