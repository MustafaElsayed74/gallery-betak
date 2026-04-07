using System.Text.Json;
using AutoMapper;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Admin;
using GalleryBetak.Application.DTOs.Order;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Application.Specifications;
using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Enums;
using GalleryBetak.Domain.Exceptions;
using GalleryBetak.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GalleryBetak.Infrastructure.Services;

/// <summary>
/// Administrative operations execution and system auditing.
/// </summary>
public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMapper _mapper;

    public AdminDashboardService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _roleManager = roleManager;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<DashboardMetricsDto>> GetDashboardMetricsAsync(CancellationToken ct = default)
    {
        var totalUsers = await _userManager.Users.CountAsync(ct);
        var totalOrders = await _unitOfWork.Orders.CountAsync(new OrderCountSpecification(), ct);
        var activeProducts = await _unitOfWork.Products.CountAsync(new ActiveProductCountSpecification(), ct);
        var pendingOrders = await _unitOfWork.Orders.CountAsync(new OrderCountSpecification(OrderStatus.Pending), ct);
        var cancelledOrders = await _unitOfWork.Orders.CountAsync(new OrderCountSpecification(OrderStatus.Cancelled), ct);
        var lowStockProducts = await _unitOfWork.Products.CountAsync(new LowStockProductsCountSpecification(2), ct);
        
        var successfulOrders = await _unitOfWork.Orders.ListAsync(new SuccessfulOrdersSpecification(), ct);
        var totalRevenue = successfulOrders.Sum(o => o.TotalAmount);

        var revenueLast30Orders = await _unitOfWork.Orders.ListAsync(new RevenueLast30DaysOrdersSpecification(), ct);
        var revenueLast30Days = revenueLast30Orders.Sum(o => o.TotalAmount);

        var openSupportMessages = await _unitOfWork.CustomerServiceMessages.CountAsync(new OpenCustomerServiceMessagesCountSpecification(), ct);
        var resolvedSupportMessages = await _unitOfWork.CustomerServiceMessages.CountAsync(new ResolvedCustomerServiceMessagesCountSpecification(), ct);

        var metrics = new DashboardMetricsDto
        {
            TotalUsers = totalUsers,
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            ActiveProducts = activeProducts,
            PendingOrders = pendingOrders,
            CancelledOrders = cancelledOrders,
            LowStockProducts = lowStockProducts,
            RevenueLast30Days = revenueLast30Days,
            OpenSupportMessages = openSupportMessages,
            ResolvedSupportMessages = resolvedSupportMessages
        };

        return ApiResponse<DashboardMetricsDto>.Ok(metrics);
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<DetailedAnalyticsDto>> GetDetailedAnalyticsAsync(int periodDays = 30, CancellationToken ct = default)
    {
        if (periodDays < 1) periodDays = 30;
        if (periodDays > 180) periodDays = 180;

        var toUtcExclusive = DateTime.UtcNow.Date.AddDays(1);
        var fromUtcInclusive = toUtcExclusive.AddDays(-periodDays);

        var orders = await _unitOfWork.Orders.ListAsync(new OrdersForAnalyticsSpecification(fromUtcInclusive, toUtcExclusive), ct);

        var revenueInPeriod = orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .Sum(o => o.TotalAmount);

        var discountsInPeriod = orders.Sum(o => o.DiscountAmount);
        var ordersInPeriod = orders.Count;
        var couponOrdersInPeriod = orders.Count(o => !string.IsNullOrWhiteSpace(o.CouponCode));
        var couponRevenueInPeriod = orders
            .Where(o => !string.IsNullOrWhiteSpace(o.CouponCode) && o.Status != OrderStatus.Cancelled)
            .Sum(o => o.TotalAmount);

        var averageOrderValue = ordersInPeriod > 0
            ? Math.Round(revenueInPeriod / ordersInPeriod, 2)
            : 0m;

        var dailyRevenue = new Dictionary<DateTime, decimal>();
        var dailyOrders = new Dictionary<DateTime, int>();
        var dailyDiscounts = new Dictionary<DateTime, decimal>();

        for (var i = 0; i < periodDays; i++)
        {
            var day = fromUtcInclusive.Date.AddDays(i);
            dailyRevenue[day] = 0m;
            dailyOrders[day] = 0;
            dailyDiscounts[day] = 0m;
        }

        foreach (var order in orders)
        {
            var day = order.CreatedAt.Date;
            if (!dailyOrders.ContainsKey(day))
            {
                continue;
            }

            dailyOrders[day] += 1;
            dailyDiscounts[day] += order.DiscountAmount;

            if (order.Status != OrderStatus.Cancelled)
            {
                dailyRevenue[day] += order.TotalAmount;
            }
        }

        var dailyTrend = dailyOrders.Keys
            .OrderBy(day => day)
            .Select(day => new AnalyticsDailyPointDto
            {
                Date = day,
                Revenue = dailyRevenue[day],
                OrdersCount = dailyOrders[day],
                DiscountAmount = dailyDiscounts[day]
            })
            .ToList();

        var orderStatusBreakdown = orders
            .GroupBy(o => o.Status)
            .OrderBy(g => g.Key)
            .Select(g => new AnalyticsOrderStatusBreakdownDto
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                Revenue = g.Key == OrderStatus.Cancelled ? 0m : g.Sum(x => x.TotalAmount)
            })
            .ToList();

        var couponPerformance = orders
            .Where(o => !string.IsNullOrWhiteSpace(o.CouponCode))
            .GroupBy(o => o.CouponCode!.Trim().ToUpperInvariant())
            .Select(g =>
            {
                var totalDiscount = g.Sum(x => x.DiscountAmount);
                return new CouponPerformanceDto
                {
                    CouponCode = g.Key,
                    OrdersCount = g.Count(),
                    TotalDiscountAmount = totalDiscount,
                    TotalRevenue = g.Where(x => x.Status != OrderStatus.Cancelled).Sum(x => x.TotalAmount),
                    AverageDiscountAmount = g.Count() > 0 ? Math.Round(totalDiscount / g.Count(), 2) : 0m
                };
            })
            .OrderByDescending(x => x.OrdersCount)
            .ThenByDescending(x => x.TotalRevenue)
            .Take(10)
            .ToList();

        var topProducts = orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { ProductId = i.ProductId ?? 0, i.ProductNameAr, i.ProductNameEn })
            .Select(g => new TopProductPerformanceDto
            {
                ProductId = g.Key.ProductId,
                ProductNameAr = g.Key.ProductNameAr,
                ProductNameEn = g.Key.ProductNameEn,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.QuantitySold)
            .ThenByDescending(x => x.Revenue)
            .Take(10)
            .ToList();

        var detailed = new DetailedAnalyticsDto
        {
            PeriodDays = periodDays,
            RevenueInPeriod = revenueInPeriod,
            OrdersInPeriod = ordersInPeriod,
            AverageOrderValue = averageOrderValue,
            DiscountsInPeriod = discountsInPeriod,
            CouponOrdersInPeriod = couponOrdersInPeriod,
            CouponRevenueInPeriod = couponRevenueInPeriod,
            DailyTrend = dailyTrend,
            OrderStatusBreakdown = orderStatusBreakdown,
            CouponPerformance = couponPerformance,
            TopProducts = topProducts
        };

        return ApiResponse<DetailedAnalyticsDto>.Ok(detailed);
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<IReadOnlyList<AuditLogDto>>> GetRecentAuditLogsAsync(int count = 50, CancellationToken ct = default)
    {
        var spec = new RecentAuditLogsSpecification(count);

        var logs = await _unitOfWork.AuditLogs.ListAsync(spec, ct);

        var dtos = logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            UserEmail = l.UserEmail,
            Action = l.Action,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            IpAddress = l.IpAddress,
            Timestamp = l.Timestamp
        }).ToList();

        return ApiResponse<IReadOnlyList<AuditLogDto>>.Ok(dtos);
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<PagedResult<UserManagementDto>>> ListUsersAsync(int pageNumber = 1, int pageSize = 20, string? search = null, CancellationToken ct = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(search)) ||
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
        }

        var totalCount = await query.CountAsync(ct);
        var users = await query.OrderByDescending(u => u.CreatedAt)
                         .Skip((pageNumber - 1) * pageSize)
                         .Take(pageSize)
                         .ToListAsync(ct);

        var dtoList = new List<UserManagementDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            dtoList.Add(MapUser(user, roles));
        }

        var pagedResult = new PagedResult<UserManagementDto>
        {
            Items = dtoList,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<UserManagementDto>>.Ok(pagedResult, pagedResult.ToMeta());
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<UserManagementDto>> CreateUserAsync(CreateAdminUserRequest request, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<UserManagementDto>.Fail(400, "بيانات المستخدم غير مكتملة", "User payload is incomplete.");
        }

        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null)
            return ApiResponse<UserManagementDto>.Fail(401, "غير مصرح", "Admin user missing.");

        var existingUser = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (existingUser != null)
            return ApiResponse<UserManagementDto>.Fail(409, "البريد الإلكتروني مستخدم بالفعل", "Email already exists.");

        var roleValidation = await ValidateRequestedRolesAsync(request.Roles);
        if (!roleValidation.IsValid)
            return ApiResponse<UserManagementDto>.Fail(400, "صلاحية غير صالحة", roleValidation.ErrorMessage!);

        var user = ApplicationUser.Create(request.Email, request.FirstName, request.LastName);
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        if (!request.IsActive)
        {
            user.Deactivate();
        }

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return ApiResponse<UserManagementDto>.Fail(400,
                "فشل إنشاء المستخدم",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        var addRolesResult = await _userManager.AddToRolesAsync(user, roleValidation.Roles);
        if (!addRolesResult.Succeeded)
        {
            return ApiResponse<UserManagementDto>.Fail(400,
                "فشل تعيين الأدوار",
                string.Join(", ", addRolesResult.Errors.Select(e => e.Description)));
        }

        var log = AuditLog.Create(
            "CreateUser",
            "ApplicationUser",
            user.Id,
            adminUser.Id,
            adminUser.Email,
            null,
            JsonSerializer.Serialize(new { user.FirstName, user.LastName, user.Email, request.IsActive, Roles = roleValidation.Roles }),
            ipAddress,
            userAgent);

        await _unitOfWork.AuditLogs.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<UserManagementDto>.Created(
            MapUser(user, roleValidation.Roles),
            "تم إنشاء المستخدم بنجاح",
            "User created successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<UserManagementDto>> UpdateUserAsync(string adminUserId, string targetUserId, UpdateAdminUserRequest request, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null)
            return ApiResponse<UserManagementDto>.Fail(401, "غير مصرح", "Admin user missing.");

        var targetUser = await _userManager.FindByIdAsync(targetUserId);
        if (targetUser == null)
            return ApiResponse<UserManagementDto>.Fail(404, "المستخدم غير موجود", "Target user not found.");

        if (adminUserId == targetUserId && !request.IsActive)
            return ApiResponse<UserManagementDto>.Fail(400, "لا يمكنك تعطيل حسابك الحالي", "You cannot deactivate your own account.");

        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return ApiResponse<UserManagementDto>.Fail(400, "بيانات المستخدم غير مكتملة", "User payload is incomplete.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!string.Equals(targetUser.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await _userManager.FindByEmailAsync(normalizedEmail);
            if (duplicate != null && duplicate.Id != targetUser.Id)
            {
                return ApiResponse<UserManagementDto>.Fail(409, "البريد الإلكتروني مستخدم بالفعل", "Email already exists.");
            }
        }

        var oldValues = JsonSerializer.Serialize(new
        {
            targetUser.FirstName,
            targetUser.LastName,
            targetUser.Email,
            targetUser.PhoneNumber,
            targetUser.IsActive
        });

        targetUser.UpdateProfile(request.FirstName.Trim(), request.LastName.Trim(), targetUser.ProfileImageUrl);
        targetUser.Email = normalizedEmail;
        targetUser.UserName = normalizedEmail;
        targetUser.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        if (request.IsActive)
        {
            targetUser.Activate();
        }
        else
        {
            targetUser.Deactivate();
        }

        var updateResult = await _userManager.UpdateAsync(targetUser);
        if (!updateResult.Succeeded)
        {
            return ApiResponse<UserManagementDto>.Fail(400,
                "فشل تحديث المستخدم",
                string.Join(", ", updateResult.Errors.Select(e => e.Description)));
        }

        var currentRoles = (await _userManager.GetRolesAsync(targetUser)).ToList();

        if (request.Roles is not null)
        {
            var roleValidation = await ValidateRequestedRolesAsync(request.Roles);
            if (!roleValidation.IsValid)
                return ApiResponse<UserManagementDto>.Fail(400, "صلاحية غير صالحة", roleValidation.ErrorMessage!);

            var desiredRoles = roleValidation.Roles;
            var rolesToRemove = currentRoles.Except(desiredRoles, StringComparer.OrdinalIgnoreCase).ToList();
            var rolesToAdd = desiredRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(targetUser, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    return ApiResponse<UserManagementDto>.Fail(400,
                        "فشل تحديث الأدوار",
                        string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                }
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(targetUser, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    return ApiResponse<UserManagementDto>.Fail(400,
                        "فشل تحديث الأدوار",
                        string.Join(", ", addResult.Errors.Select(e => e.Description)));
                }
            }

            currentRoles = (await _userManager.GetRolesAsync(targetUser)).ToList();
        }

        var newValues = JsonSerializer.Serialize(new
        {
            targetUser.FirstName,
            targetUser.LastName,
            targetUser.Email,
            targetUser.PhoneNumber,
            targetUser.IsActive,
            Roles = currentRoles
        });

        var log = AuditLog.Create(
            "UpdateUser",
            "ApplicationUser",
            targetUser.Id,
            adminUser.Id,
            adminUser.Email,
            oldValues,
            newValues,
            ipAddress,
            userAgent);

        await _unitOfWork.AuditLogs.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<UserManagementDto>.Ok(
            MapUser(targetUser, currentRoles),
            "تم تحديث المستخدم بنجاح",
            "User updated successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> SetUserStatusAsync(string adminUserId, string targetUserId, bool isActive, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null)
            return ApiResponse<bool>.Fail(401, "غير مصرح", "Admin user missing.");

        var targetUser = await _userManager.FindByIdAsync(targetUserId);
        if (targetUser == null)
            return ApiResponse<bool>.Fail(404, "المستخدم غير موجود", "Target user not found.");

        if (adminUserId == targetUserId && !isActive)
            return ApiResponse<bool>.Fail(400, "لا يمكنك تعطيل حسابك الحالي", "You cannot deactivate your own account.");

        var oldValues = JsonSerializer.Serialize(new { targetUser.IsActive });

        if (isActive)
            targetUser.Activate();
        else
            targetUser.Deactivate();

        var result = await _userManager.UpdateAsync(targetUser);
        if (!result.Succeeded)
        {
            return ApiResponse<bool>.Fail(400,
                "فشل تحديث حالة المستخدم",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var log = AuditLog.Create(
            "SetUserStatus",
            "ApplicationUser",
            targetUser.Id,
            adminUser.Id,
            adminUser.Email,
            oldValues,
            JsonSerializer.Serialize(new { targetUser.IsActive }),
            ipAddress,
            userAgent);

        await _unitOfWork.AuditLogs.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true,
            isActive ? "تم تفعيل المستخدم" : "تم تعطيل المستخدم",
            isActive ? "User activated successfully." : "User deactivated successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> DeleteUserAsync(string adminUserId, string targetUserId, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null)
            return ApiResponse<bool>.Fail(401, "غير مصرح", "Admin user missing.");

        var targetUser = await _userManager.FindByIdAsync(targetUserId);
        if (targetUser == null)
            return ApiResponse<bool>.Fail(404, "المستخدم غير موجود", "Target user not found.");

        if (adminUserId == targetUserId)
            return ApiResponse<bool>.Fail(400, "لا يمكنك حذف حسابك الحالي", "You cannot delete your own account.");

        var oldValues = JsonSerializer.Serialize(new
        {
            targetUser.FirstName,
            targetUser.LastName,
            targetUser.Email,
            targetUser.IsActive,
            targetUser.IsDeleted
        });

        targetUser.MarkDeleted();
        targetUser.RevokeRefreshToken();

        var result = await _userManager.UpdateAsync(targetUser);
        if (!result.Succeeded)
        {
            return ApiResponse<bool>.Fail(400,
                "فشل حذف المستخدم",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var log = AuditLog.Create(
            "DeleteUser",
            "ApplicationUser",
            targetUser.Id,
            adminUser.Id,
            adminUser.Email,
            oldValues,
            JsonSerializer.Serialize(new { targetUser.IsDeleted, targetUser.IsActive }),
            ipAddress,
            userAgent);

        await _unitOfWork.AuditLogs.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true, "تم حذف المستخدم بنجاح", "User deleted successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> AssignRoleAsync(string adminUserId, string targetUserId, string role, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(role))
            return ApiResponse<bool>.Fail(400, "الدور مطلوب", "Role is required.");

        if (!await _roleManager.RoleExistsAsync(role))
            return ApiResponse<bool>.Fail(400, "الدور غير موجود", $"Role '{role}' does not exist.");

        var targetUser = await _userManager.FindByIdAsync(targetUserId);
        if (targetUser == null)
            return ApiResponse<bool>.Fail(404, "المستخدم غير موجود", "Target user not found.");

        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null) return ApiResponse<bool>.Fail(401, "غير مصرح", "Admin user missing.");

        var existingRoles = await _userManager.GetRolesAsync(targetUser);

        if (!existingRoles.Contains(role))
        {
            var result = await _userManager.AddToRoleAsync(targetUser, role);
            if (!result.Succeeded)
                return ApiResponse<bool>.Fail(400, "فشل في تعيين الصلاحية", string.Join(", ", result.Errors.Select(e => e.Description)));

            var log = AuditLog.Create("AssignRole", "ApplicationUser", targetUser.Id, adminUser.Id, adminUser.Email,
                JsonSerializer.Serialize(existingRoles), JsonSerializer.Serialize(new[] { role }), ipAddress, userAgent);

            await _unitOfWork.AuditLogs.AddAsync(log, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return ApiResponse<bool>.Ok(true, "تم تعيين الصلاحية", "Role assigned successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<PagedResult<CouponAdminDto>>> ListCouponsAsync(int pageNumber = 1, int pageSize = 20, string? search = null, bool? isActive = null, CancellationToken ct = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var skip = (pageNumber - 1) * pageSize;

        var coupons = await _unitOfWork.Coupons.ListAsync(new AdminCouponsSpecification(skip, pageSize, normalizedSearch, isActive), ct);
        var totalCount = await _unitOfWork.Coupons.CountAsync(new AdminCouponsCountSpecification(normalizedSearch, isActive), ct);

        var paged = new PagedResult<CouponAdminDto>
        {
            Items = coupons.Select(MapCoupon).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<CouponAdminDto>>.Ok(paged, paged.ToMeta());
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<CouponAdminDto>> CreateCouponAsync(CreateCouponRequest request, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null)
            return ApiResponse<CouponAdminDto>.Fail(401, "غير مصرح", "Admin user missing.");

        if (string.IsNullOrWhiteSpace(request.Code))
            return ApiResponse<CouponAdminDto>.Fail(400, "كود الكوبون مطلوب", "Coupon code is required.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _unitOfWork.Coupons.AnyAsync(c => c.Code == normalizedCode, ct);
        if (codeExists)
            return ApiResponse<CouponAdminDto>.Fail(409, "كود الكوبون مستخدم بالفعل", "Coupon code already exists.");

        try
        {
            var coupon = Coupon.Create(
                normalizedCode,
                request.DiscountType,
                request.DiscountValue,
                request.StartsAt,
                request.ExpiresAt,
                request.MinOrderAmount,
                request.MaxDiscountAmount,
                request.UsageLimit);

            coupon.SetDescriptions(request.DescriptionAr, request.DescriptionEn);
            coupon.SetActive(request.IsActive);

            await _unitOfWork.Coupons.AddAsync(coupon, ct);

            var log = AuditLog.Create(
                "CreateCoupon",
                "Coupon",
                coupon.Code,
                adminUser.Id,
                adminUser.Email,
                null,
                JsonSerializer.Serialize(new
                {
                    coupon.Code,
                    DiscountType = coupon.DiscountType.ToString(),
                    coupon.DiscountValue,
                    coupon.MinOrderAmount,
                    coupon.MaxDiscountAmount,
                    coupon.UsageLimit,
                    coupon.StartsAt,
                    coupon.ExpiresAt,
                    coupon.IsActive
                }),
                ipAddress,
                userAgent);

            await _unitOfWork.AuditLogs.AddAsync(log, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<CouponAdminDto>.Created(
                MapCoupon(coupon),
                "تم إنشاء الكوبون بنجاح",
                "Coupon created successfully.");
        }
        catch (DomainException ex)
        {
            return ApiResponse<CouponAdminDto>.Fail(400, ex.Message, ex.MessageEn ?? ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<CouponAdminDto>> UpdateCouponAsync(int couponId, UpdateCouponRequest request, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null)
            return ApiResponse<CouponAdminDto>.Fail(401, "غير مصرح", "Admin user missing.");

        var coupon = await _unitOfWork.Coupons.GetByIdAsync(couponId, ct);
        if (coupon == null)
            return ApiResponse<CouponAdminDto>.Fail(404, "الكوبون غير موجود", "Coupon not found.");

        if (string.IsNullOrWhiteSpace(request.Code))
            return ApiResponse<CouponAdminDto>.Fail(400, "كود الكوبون مطلوب", "Coupon code is required.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var duplicateCode = await _unitOfWork.Coupons.AnyAsync(c => c.Id != couponId && c.Code == normalizedCode, ct);
        if (duplicateCode)
            return ApiResponse<CouponAdminDto>.Fail(409, "كود الكوبون مستخدم بالفعل", "Coupon code already exists.");

        var oldValues = JsonSerializer.Serialize(new
        {
            coupon.Code,
            DiscountType = coupon.DiscountType.ToString(),
            coupon.DiscountValue,
            coupon.MinOrderAmount,
            coupon.MaxDiscountAmount,
            coupon.UsageLimit,
            coupon.UsedCount,
            coupon.StartsAt,
            coupon.ExpiresAt,
            coupon.IsActive
        });

        try
        {
            coupon.Update(
                normalizedCode,
                request.DiscountType,
                request.DiscountValue,
                request.StartsAt,
                request.ExpiresAt,
                request.MinOrderAmount,
                request.MaxDiscountAmount,
                request.UsageLimit);

            coupon.SetDescriptions(request.DescriptionAr, request.DescriptionEn);
            coupon.SetActive(request.IsActive);

            _unitOfWork.Coupons.Update(coupon);

            var log = AuditLog.Create(
                "UpdateCoupon",
                "Coupon",
                coupon.Id.ToString(),
                adminUser.Id,
                adminUser.Email,
                oldValues,
                JsonSerializer.Serialize(new
                {
                    coupon.Code,
                    DiscountType = coupon.DiscountType.ToString(),
                    coupon.DiscountValue,
                    coupon.MinOrderAmount,
                    coupon.MaxDiscountAmount,
                    coupon.UsageLimit,
                    coupon.UsedCount,
                    coupon.StartsAt,
                    coupon.ExpiresAt,
                    coupon.IsActive
                }),
                ipAddress,
                userAgent);

            await _unitOfWork.AuditLogs.AddAsync(log, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<CouponAdminDto>.Ok(
                MapCoupon(coupon),
                "تم تحديث الكوبون بنجاح",
                "Coupon updated successfully.");
        }
        catch (DomainException ex)
        {
            return ApiResponse<CouponAdminDto>.Fail(400, ex.Message, ex.MessageEn ?? ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> SetCouponStatusAsync(int couponId, bool isActive, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null)
            return ApiResponse<bool>.Fail(401, "غير مصرح", "Admin user missing.");

        var coupon = await _unitOfWork.Coupons.GetByIdAsync(couponId, ct);
        if (coupon == null)
            return ApiResponse<bool>.Fail(404, "الكوبون غير موجود", "Coupon not found.");

        var oldValues = JsonSerializer.Serialize(new { coupon.IsActive });

        coupon.SetActive(isActive);
        _unitOfWork.Coupons.Update(coupon);

        var log = AuditLog.Create(
            "SetCouponStatus",
            "Coupon",
            coupon.Id.ToString(),
            adminUser.Id,
            adminUser.Email,
            oldValues,
            JsonSerializer.Serialize(new { coupon.IsActive }),
            ipAddress,
            userAgent);

        await _unitOfWork.AuditLogs.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(
            true,
            isActive ? "تم تفعيل الكوبون" : "تم تعطيل الكوبون",
            isActive ? "Coupon activated successfully." : "Coupon deactivated successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> DeleteCouponAsync(int couponId, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null)
            return ApiResponse<bool>.Fail(401, "غير مصرح", "Admin user missing.");

        var coupon = await _unitOfWork.Coupons.GetByIdAsync(couponId, ct);
        if (coupon == null)
            return ApiResponse<bool>.Fail(404, "الكوبون غير موجود", "Coupon not found.");

        var oldValues = JsonSerializer.Serialize(new
        {
            coupon.Code,
            coupon.IsActive,
            coupon.UsedCount,
            coupon.UsageLimit
        });

        coupon.SoftDelete();
        _unitOfWork.Coupons.Update(coupon);

        var log = AuditLog.Create(
            "DeleteCoupon",
            "Coupon",
            coupon.Id.ToString(),
            adminUser.Id,
            adminUser.Email,
            oldValues,
            JsonSerializer.Serialize(new { coupon.IsDeleted, coupon.DeletedAt }),
            ipAddress,
            userAgent);

        await _unitOfWork.AuditLogs.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true, "تم حذف الكوبون بنجاح", "Coupon deleted successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<PagedResult<OfferAdminDto>>> ListDiscountOffersAsync(int pageNumber = 1, int pageSize = 20, string? search = null, bool? activeOnly = null, CancellationToken ct = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var skip = (pageNumber - 1) * pageSize;

        var offers = await _unitOfWork.Products.ListAsync(new DiscountOffersSpecification(skip, pageSize, normalizedSearch, activeOnly), ct);
        var totalCount = await _unitOfWork.Products.CountAsync(new DiscountOffersCountSpecification(normalizedSearch, activeOnly), ct);

        var paged = new PagedResult<OfferAdminDto>
        {
            Items = offers.Select(MapOffer).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<OfferAdminDto>>.Ok(paged, paged.ToMeta());
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<OfferAdminDto>> ApplyDiscountToProductAsync(int productId, ApplyProductDiscountRequest request, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null)
            return ApiResponse<OfferAdminDto>.Fail(401, "غير مصرح", "Admin user missing.");

        var product = await _unitOfWork.Products.GetByIdAsync(productId, ct);
        if (product == null)
            return ApiResponse<OfferAdminDto>.Fail(404, "المنتج غير موجود", "Product not found.");

        if (!request.DiscountedPrice.HasValue && !request.DiscountPercentage.HasValue)
            return ApiResponse<OfferAdminDto>.Fail(400, "يرجى تحديد سعر الخصم أو نسبة الخصم", "Either discounted price or discount percentage is required.");

        if (request.DiscountedPrice.HasValue && request.DiscountPercentage.HasValue)
            return ApiResponse<OfferAdminDto>.Fail(400, "استخدم سعرًا مخفضًا أو نسبة خصم فقط", "Use either discounted price or discount percentage, not both.");

        var oldValues = JsonSerializer.Serialize(new { product.Price, product.OriginalPrice, product.HasDiscount, product.DiscountPercentage });

        try
        {
            var basePrice = product.OriginalPrice ?? product.Price;
            decimal discountedPrice;

            if (request.DiscountedPrice.HasValue)
            {
                discountedPrice = request.DiscountedPrice.Value;
                if (discountedPrice <= 0)
                    return ApiResponse<OfferAdminDto>.Fail(400, "سعر الخصم يجب أن يكون أكبر من صفر", "Discounted price must be greater than zero.");
                if (discountedPrice >= basePrice)
                    return ApiResponse<OfferAdminDto>.Fail(400, "سعر الخصم يجب أن يكون أقل من السعر الأصلي", "Discounted price must be less than original price.");
            }
            else
            {
                var percentage = request.DiscountPercentage!.Value;
                if (percentage <= 0 || percentage >= 100)
                    return ApiResponse<OfferAdminDto>.Fail(400, "نسبة الخصم يجب أن تكون بين 1 و 99", "Discount percentage must be between 1 and 99.");

                discountedPrice = Math.Round(basePrice * (1 - (percentage / 100m)), 2, MidpointRounding.AwayFromZero);
                if (discountedPrice <= 0)
                    return ApiResponse<OfferAdminDto>.Fail(400, "نسبة الخصم الحالية تؤدي إلى سعر غير صالح", "The current discount percentage results in an invalid price.");
            }

            product.UpdatePrice(discountedPrice, basePrice);
            _unitOfWork.Products.Update(product);

            var log = AuditLog.Create(
                "ApplyProductDiscount",
                "Product",
                product.Id.ToString(),
                adminUser.Id,
                adminUser.Email,
                oldValues,
                JsonSerializer.Serialize(new { product.Price, product.OriginalPrice, product.HasDiscount, product.DiscountPercentage }),
                ipAddress,
                userAgent);

            await _unitOfWork.AuditLogs.AddAsync(log, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var productWithCategory = await _unitOfWork.Products.GetEntityWithSpecAsync(new ProductWithCategorySpecification(product.Id), ct);
            var mapped = MapOffer(productWithCategory ?? product);

            return ApiResponse<OfferAdminDto>.Ok(
                mapped,
                "تم تطبيق الخصم على المنتج",
                "Product discount applied successfully.");
        }
        catch (DomainException ex)
        {
            return ApiResponse<OfferAdminDto>.Fail(400, ex.Message, ex.MessageEn ?? ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> ClearProductDiscountAsync(int productId, string adminUserId, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null)
            return ApiResponse<bool>.Fail(401, "غير مصرح", "Admin user missing.");

        var product = await _unitOfWork.Products.GetByIdAsync(productId, ct);
        if (product == null)
            return ApiResponse<bool>.Fail(404, "المنتج غير موجود", "Product not found.");

        if (!product.HasDiscount)
            return ApiResponse<bool>.Ok(true, "المنتج لا يحتوي على خصم نشط", "Product does not have an active discount.");

        var oldValues = JsonSerializer.Serialize(new { product.Price, product.OriginalPrice, product.HasDiscount, product.DiscountPercentage });

        try
        {
            product.UpdatePrice(product.OriginalPrice ?? product.Price);
            _unitOfWork.Products.Update(product);

            var log = AuditLog.Create(
                "ClearProductDiscount",
                "Product",
                product.Id.ToString(),
                adminUser.Id,
                adminUser.Email,
                oldValues,
                JsonSerializer.Serialize(new { product.Price, product.OriginalPrice, product.HasDiscount, product.DiscountPercentage }),
                ipAddress,
                userAgent);

            await _unitOfWork.AuditLogs.AddAsync(log, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<bool>.Ok(true, "تمت إزالة الخصم بنجاح", "Discount cleared successfully.");
        }
        catch (DomainException ex)
        {
            return ApiResponse<bool>.Fail(400, ex.Message, ex.MessageEn ?? ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<PagedResult<OrderSummaryDto>>> ListOrdersAsync(int pageNumber = 1, int pageSize = 20, OrderStatus? status = null, CancellationToken ct = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        var skip = (pageNumber - 1) * pageSize;
        var spec = new AdminOrdersSpecification(skip, pageSize, status);
        var countSpec = new AdminOrdersCountSpecification(status);

        var orders = await _unitOfWork.Orders.ListAsync(spec, ct);
        var totalCount = await _unitOfWork.Orders.CountAsync(countSpec, ct);

        var paged = new PagedResult<OrderSummaryDto>
        {
            Items = _mapper.Map<IReadOnlyList<OrderSummaryDto>>(orders),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<OrderSummaryDto>>.Ok(paged, paged.ToMeta());
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<IReadOnlyList<OrderSummaryDto>>> GetRecentOrdersAsync(int count = 10, CancellationToken ct = default)
    {
        var spec = new RecentOrdersSpecification(count);

        var orders = await _unitOfWork.Orders.ListAsync(spec, ct);
        return ApiResponse<IReadOnlyList<OrderSummaryDto>>.Ok(_mapper.Map<IReadOnlyList<OrderSummaryDto>>(orders));
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<PagedResult<CustomerServiceMessageDto>>> ListCustomerServiceMessagesAsync(int pageNumber = 1, int pageSize = 20, CustomerServiceMessageStatus? status = null, string? search = null, CancellationToken ct = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        var skip = (pageNumber - 1) * pageSize;
        var spec = new CustomerServiceMessagesSpecification(skip, pageSize, status, search);
        var countSpec = new CustomerServiceMessagesCountSpecification(status, search);

        var messages = await _unitOfWork.CustomerServiceMessages.ListAsync(spec, ct);
        var totalCount = await _unitOfWork.CustomerServiceMessages.CountAsync(countSpec, ct);

        var paged = new PagedResult<CustomerServiceMessageDto>
        {
            Items = messages.Select(MapCustomerServiceMessage).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<CustomerServiceMessageDto>>.Ok(paged, paged.ToMeta());
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<CustomerServiceMessageDto>> CreateCustomerServiceMessageAsync(CreateCustomerServiceMessageRequest request, CancellationToken ct = default)
    {
        try
        {
            var message = CustomerServiceMessage.Create(
                request.Name,
                request.Email,
                request.PhoneNumber,
                request.Subject,
                request.Message);

            await _unitOfWork.CustomerServiceMessages.AddAsync(message, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<CustomerServiceMessageDto>.Created(
                MapCustomerServiceMessage(message),
                "تم إرسال رسالتك بنجاح",
                "Your message has been submitted successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<CustomerServiceMessageDto>.Fail(400, ex.Message, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<CustomerServiceMessageDto>> UpdateCustomerServiceMessageAsync(string adminUserId, int messageId, UpdateCustomerServiceMessageRequest request, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null)
            return ApiResponse<CustomerServiceMessageDto>.Fail(401, "غير مصرح", "Admin user missing.");

        var message = await _unitOfWork.CustomerServiceMessages.GetByIdAsync(messageId, ct);
        if (message == null)
            return ApiResponse<CustomerServiceMessageDto>.Fail(404, "الرسالة غير موجودة", "Message not found.");

        var oldValues = JsonSerializer.Serialize(new { message.Status, message.AdminNotes, message.HandledByUserId, message.ResolvedAt });

        message.UpdateStatus(request.Status, request.AdminNotes, adminUserId);

        var log = AuditLog.Create(
            "UpdateSupportMessage",
            "CustomerServiceMessage",
            message.Id.ToString(),
            adminUser.Id,
            adminUser.Email,
            oldValues,
            JsonSerializer.Serialize(new { message.Status, message.AdminNotes, message.HandledByUserId, message.ResolvedAt }),
            ipAddress,
            userAgent);

        await _unitOfWork.AuditLogs.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<CustomerServiceMessageDto>.Ok(
            MapCustomerServiceMessage(message),
            "تم تحديث الرسالة بنجاح",
            "Support message updated successfully.");
    }

    private static UserManagementDto MapUser(ApplicationUser user, IEnumerable<string> roles)
    {
        return new UserManagementDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles.ToList()
        };
    }

    private static CustomerServiceMessageDto MapCustomerServiceMessage(CustomerServiceMessage message)
    {
        return new CustomerServiceMessageDto
        {
            Id = message.Id,
            Name = message.Name,
            Email = message.Email,
            PhoneNumber = message.PhoneNumber,
            Subject = message.Subject,
            Message = message.Message,
            Status = message.Status.ToString(),
            AdminNotes = message.AdminNotes,
            HandledByUserId = message.HandledByUserId,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            ResolvedAt = message.ResolvedAt
        };
    }

    private static CouponAdminDto MapCoupon(Coupon coupon)
    {
        return new CouponAdminDto
        {
            Id = coupon.Id,
            Code = coupon.Code,
            DescriptionAr = coupon.DescriptionAr,
            DescriptionEn = coupon.DescriptionEn,
            DiscountType = coupon.DiscountType.ToString(),
            DiscountValue = coupon.DiscountValue,
            MinOrderAmount = coupon.MinOrderAmount,
            MaxDiscountAmount = coupon.MaxDiscountAmount,
            UsageLimit = coupon.UsageLimit,
            UsedCount = coupon.UsedCount,
            StartsAt = coupon.StartsAt,
            ExpiresAt = coupon.ExpiresAt,
            IsActive = coupon.IsActive,
            IsValid = coupon.IsValid,
            CreatedAt = coupon.CreatedAt
        };
    }

    private static OfferAdminDto MapOffer(Product product)
    {
        return new OfferAdminDto
        {
            ProductId = product.Id,
            ProductNameAr = product.NameAr,
            ProductNameEn = product.NameEn,
            Slug = product.Slug,
            Price = product.Price,
            OriginalPrice = product.OriginalPrice,
            DiscountPercentage = product.DiscountPercentage,
            StockQuantity = product.StockQuantity,
            IsActive = product.IsActive,
            CategoryNameAr = product.Category?.NameAr,
            CategoryNameEn = product.Category?.NameEn
        };
    }

    private async Task<(bool IsValid, string? ErrorMessage, List<string> Roles)> ValidateRequestedRolesAsync(IReadOnlyList<string>? requestedRoles)
    {
        var roles = (requestedRoles ?? [])
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (roles.Count == 0)
        {
            roles.Add("Customer");
        }

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return (false, $"Role '{role}' does not exist.", roles);
            }
        }

        return (true, null, roles);
    }
}

