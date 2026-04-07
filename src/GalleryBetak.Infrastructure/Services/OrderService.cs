using AutoMapper;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Order;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Application.Specifications;
using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Enums;
using GalleryBetak.Domain.Exceptions;
using GalleryBetak.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace GalleryBetak.Infrastructure.Services;

/// <summary>
/// Domain-driven order logic execution.
/// Transfers the Cart state into a Frozen Order state, triggers domain events, and clears the cart.
/// </summary>
public sealed class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartService _cartService;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrderService(
        IUnitOfWork unitOfWork,
        ICartService cartService,
        IMapper mapper,
        UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _cartService = cartService;
        _mapper = mapper;
        _userManager = userManager;
    }

    private string GenerateOrderNumber()
    {
        // Simple ORD-YYYYMM-XXXXX generator. Could use sequence in DB for production.
        var datePart = DateTime.UtcNow.ToString("yyyyMM");
        var randomPart = new Random().Next(10000, 99999);
        return $"ORD-{datePart}-{randomPart}";
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<OrderDto>> CreateOrderAsync(string userId, CreateOrderRequest request, CancellationToken ct = default)
    {
        // 1. Validate Address
        var address = await _unitOfWork.Addresses.GetByIdAsync(request.AddressId, ct);
        if (address == null || address.UserId != userId)
            return ApiResponse<OrderDto>.Fail(400, "العنوان غير صحيح", "Invalid address.");

        // 2. Extract Cart
        var cartSpec = new CartWithItemsSpecification(userId, true);
        var cart = await _unitOfWork.Carts.GetEntityWithSpecAsync(cartSpec, ct);

        if (cart == null || cart.IsEmpty)
            return ApiResponse<OrderDto>.Fail(400, "سلة المشتريات فارغة", "Cart is empty.");

        // 3. Financials Mock Logic
        decimal shippingCost = request.PaymentMethod == PaymentMethod.COD ? 40m : 30m;
        decimal taxAmount = cart.SubTotal * 0.14m; // 14% Egypt VAT

        // 4. Create Order Root
        var orderNumber = GenerateOrderNumber();
        var order = Order.Create(userId, orderNumber, address, request.PaymentMethod, request.Notes);

        // 5. Transfer Cart Items to Order Items + Reduce Stock
        var lowStockProducts = new List<Product>();
        foreach (var cartItem in cart.Items)
        {
            // Verify stock again if needed...
            if (cartItem.Product.StockQuantity < cartItem.Quantity)
                return ApiResponse<OrderDto>.Fail(400, $"الكمية المطلوبة لـ {cartItem.Product.NameAr} تفوق المخزون المتاح", $"Insufficient stock for {cartItem.Product.NameEn}.");

            var orderItem = OrderItem.Create(cartItem.Product, cartItem.Quantity);
            order.AddItem(orderItem);

            // Reduce stock from product
            cartItem.Product.ReduceStock(cartItem.Quantity);

            // Trigger low-stock alert when the product reaches exactly 2 units.
            if (cartItem.Product.StockQuantity == 2)
            {
                lowStockProducts.Add(cartItem.Product);
            }
        }

        // Apply Financials
        order.SetFinancials(shippingCost, taxAmount, 0m);

        // Finalize
        order.PlaceOrder();

        // Save order structure
        await _unitOfWork.Orders.AddAsync(order, ct);

        // Dispatch low-stock admin alerts for products that reached the threshold.
        await CreateLowStockAlertsAsync(lowStockProducts, ct);

        // Clear cart now that atomic execution has succeeded up to the UnitOfWork
        cart.Clear();

        await _unitOfWork.SaveChangesAsync(ct);

        // Load to map accurately
        var resultingOrderSpec = new OrderWithItemsSpecification(order.Id, userId);
        var resultingOrder = await _unitOfWork.Orders.GetEntityWithSpecAsync(resultingOrderSpec, ct);

        return ApiResponse<OrderDto>.Created(
            _mapper.Map<OrderDto>(resultingOrder!),
            "تم استلام الطلب بنجاح", "Order placed successfully.");
    }

    private async Task CreateLowStockAlertsAsync(IReadOnlyCollection<Product> lowStockProducts, CancellationToken ct)
    {
        if (lowStockProducts.Count == 0)
        {
            return;
        }

        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");

        var recipients = admins
            .Concat(superAdmins)
            .Where(user => user.IsActive && !user.IsDeleted)
            .GroupBy(user => user.Id)
            .Select(group => group.First())
            .ToList();

        if (recipients.Count == 0)
        {
            return;
        }

        var uniqueLowStockProducts = lowStockProducts
            .GroupBy(product => product.Id)
            .Select(group => group.First());

        foreach (var product in uniqueLowStockProducts)
        {
            var payload = JsonSerializer.Serialize(new
            {
                ProductId = product.Id,
                ProductNameAr = product.NameAr,
                ProductNameEn = product.NameEn,
                RemainingStock = product.StockQuantity,
                AlertType = "LowStock",
                TriggerStock = 2
            });

            foreach (var recipient in recipients)
            {
                var auditLog = AuditLog.Create(
                    "LowStockAlert",
                    "Product",
                    product.Id.ToString(),
                    recipient.Id,
                    recipient.Email,
                    null,
                    payload,
                    "System",
                    "OrderService");

                await _unitOfWork.AuditLogs.AddAsync(auditLog, ct);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<OrderDto>> GetOrderByIdAsync(int id, string userId, CancellationToken ct = default)
    {
        var spec = new OrderWithItemsSpecification(id, userId);
        var order = await _unitOfWork.Orders.GetEntityWithSpecAsync(spec, ct);

        if (order is null)
            return ApiResponse<OrderDto>.Fail(404, "الطلب غير موجود", "Order not found.");

        return ApiResponse<OrderDto>.Ok(_mapper.Map<OrderDto>(order));
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<IReadOnlyList<OrderSummaryDto>>> GetUserOrdersAsync(string userId, CancellationToken ct = default)
    {
        var spec = new OrderWithItemsSpecification(userId);
        var orders = await _unitOfWork.Orders.ListAsync(spec, ct);

        return ApiResponse<IReadOnlyList<OrderSummaryDto>>.Ok(
            _mapper.Map<IReadOnlyList<OrderSummaryDto>>(orders));
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> UpdateOrderStatusAsync(int id, OrderStatus newStatus, string? trackingNumber = null, string? cancelReason = null, CancellationToken ct = default)
    {
        var spec = new OrderWithItemsSpecification(id); // Admin override spec
        var order = await _unitOfWork.Orders.GetEntityWithSpecAsync(spec, ct);

        if (order is null)
            return ApiResponse<bool>.Fail(404, "الطلب غير موجود", "Order not found.");

        try
        {
            switch (newStatus)
            {
                case OrderStatus.Confirmed:
                    order.Confirm();
                    break;
                case OrderStatus.Processing:
                    order.StartProcessing();
                    break;
                case OrderStatus.Shipped:
                    if (string.IsNullOrWhiteSpace(trackingNumber))
                        return ApiResponse<bool>.Fail(400, "رقم التتبع مطلوب", "Tracking number required for shipping.");
                    order.Ship(trackingNumber);
                    break;
                case OrderStatus.Delivered:
                    order.Deliver();
                    break;
                case OrderStatus.Cancelled:
                    if (string.IsNullOrWhiteSpace(cancelReason))
                        return ApiResponse<bool>.Fail(400, "سبب الإلغاء مطلوب", "Cancel reason required.");
                    order.Cancel(cancelReason);
                    break;
                case OrderStatus.Refunded:
                    order.Refund();
                    break;
                default:
                    return ApiResponse<bool>.Fail(400, "حالة غير صالحة", "Invalid status.");
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return ApiResponse<bool>.Ok(true, "تم تحديث حالة الطلب", "Status updated successfully.");
        }
        catch (BusinessRuleException ex)
        {
            return ApiResponse<bool>.Fail(400, ex.Message, ex.MessageEn ?? "Business rule violation.");
        }
    }
}

