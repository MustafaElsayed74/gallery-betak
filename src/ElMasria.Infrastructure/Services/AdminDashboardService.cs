using System.Text.Json;
using AutoMapper;
using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Admin;
using ElMasria.Application.DTOs.Order;
using ElMasria.Application.Interfaces;
using ElMasria.Application.Specifications;
using ElMasria.Domain.Entities;
using ElMasria.Domain.Enums;
using ElMasria.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ElMasria.Infrastructure.Services;

/// <summary>
/// Administrative operations execution and system auditing.
/// </summary>
public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public AdminDashboardService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<DashboardMetricsDto>> GetDashboardMetricsAsync(CancellationToken ct = default)
    {
        // UserManager does not require BaseSpecification
        var totalUsers = _userManager.Users.Count();
        
        var totalOrders = await _unitOfWork.Orders.CountAsync(new OrderCountSpecification(), ct);
        var activeProducts = await _unitOfWork.Products.CountAsync(new ActiveProductCountSpecification(), ct);
        var pendingOrders = await _unitOfWork.Orders.CountAsync(new OrderCountSpecification(OrderStatus.Pending), ct);
        
        var orders = await _unitOfWork.Orders.ListAsync(new SuccessfulOrdersSpecification(), ct);
        var revenue = orders.Sum(o => o.TotalAmount);

        var metrics = new DashboardMetricsDto
        {
            TotalUsers = totalUsers,
            TotalOrders = totalOrders,
            TotalRevenue = revenue,
            ActiveProducts = activeProducts,
            PendingOrders = pendingOrders
        };

        return ApiResponse<DashboardMetricsDto>.Ok(metrics);
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
        // Simple IQueryable projection since UserManager doesn't wire into Specification Pattern naturally easily
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Email!.Contains(search) || u.FirstName.Contains(search) || u.LastName.Contains(search));
        }

        var totalCount = query.Count();
        var users = query.OrderByDescending(u => u.CreatedAt)
                         .Skip((pageNumber - 1) * pageSize)
                         .Take(pageSize)
                         .ToList();

        var dtoList = new List<UserManagementDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            dtoList.Add(new UserManagementDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsActive = user.IsActive,
                Roles = roles.ToList()
            });
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
    public async Task<ApiResponse<bool>> AssignRoleAsync(string adminUserId, string targetUserId, string role, string ipAddress, string userAgent, CancellationToken ct = default)
    {
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
    public async Task<ApiResponse<IReadOnlyList<OrderSummaryDto>>> GetRecentOrdersAsync(int count = 10, CancellationToken ct = default)
    {
        var spec = new RecentOrdersSpecification(count);

        var orders = await _unitOfWork.Orders.ListAsync(spec, ct);
        return ApiResponse<IReadOnlyList<OrderSummaryDto>>.Ok(_mapper.Map<IReadOnlyList<OrderSummaryDto>>(orders));
    }
}
