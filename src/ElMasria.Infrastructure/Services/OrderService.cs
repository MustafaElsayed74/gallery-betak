using AutoMapper;
using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Order;
using ElMasria.Application.Interfaces;
using ElMasria.Application.Specifications;
using ElMasria.Domain.Entities;
using ElMasria.Domain.Enums;
using ElMasria.Domain.Exceptions;
using ElMasria.Domain.Interfaces;

namespace ElMasria.Infrastructure.Services;

/// <summary>
/// Domain-driven order logic execution.
/// Transfers the Cart state into a Frozen Order state, triggers domain events, and clears the cart.
/// </summary>
public sealed class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartService _cartService;
    private readonly IMapper _mapper;

    public OrderService(IUnitOfWork unitOfWork, ICartService cartService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _cartService = cartService;
        _mapper = mapper;
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
        decimal shippingCost = cart.SubTotal >= 1000 ? 0m : 50m;
        decimal taxAmount = cart.SubTotal * 0.14m; // 14% Egypt VAT

        // 4. Create Order Root
        var orderNumber = GenerateOrderNumber();
        var order = Order.Create(userId, orderNumber, address, request.PaymentMethod, request.Notes);

        // 5. Transfer Cart Items to Order Items
        foreach (var cartItem in cart.Items)
        {
            // Verify stock again if needed...
            if (cartItem.Product.StockQuantity < cartItem.Quantity)
                return ApiResponse<OrderDto>.Fail(400, $"الكمية المطلوبة لـ {cartItem.Product.NameAr} تفوق المخزون المتاح", $"Insufficient stock for {cartItem.Product.NameEn}.");

            var orderItem = OrderItem.Create(cartItem.Product, cartItem.Quantity);
            order.AddItem(orderItem);
        }

        // Apply Financials
        order.SetFinancials(shippingCost, taxAmount, 0m);

        // Finalize
        order.PlaceOrder();

        // Save order structure
        await _unitOfWork.Orders.AddAsync(order, ct);

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
