using AutoMapper;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Cart;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Application.Specifications;
using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Interfaces;

namespace GalleryBetak.Infrastructure.Services;

/// <summary>
/// Implementation of shopping cart logic. Handles guest tracking and cart merging.
/// </summary>
public sealed class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CartService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>Gets or initializes the active cart for the requester.</summary>
    private async Task<Cart> GetOrInitializeCartAsync(string? userId, string? sessionId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Must provide either UserId or SessionId for cart.");

        Cart? cart = null;

        if (!string.IsNullOrEmpty(userId))
        {
            var spec = new CartWithItemsSpecification(userId, true);
            cart = await _unitOfWork.Carts.GetEntityWithSpecAsync(spec, ct);
            if (cart is null)
            {
                cart = Cart.CreateForUser(userId);
                await _unitOfWork.Carts.AddAsync(cart, ct);
            }
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            var spec = new CartWithItemsSpecification(sessionId, 0);
            cart = await _unitOfWork.Carts.GetEntityWithSpecAsync(spec, ct);
            if (cart is null)
            {
                cart = Cart.CreateForGuest(sessionId);
                await _unitOfWork.Carts.AddAsync(cart, ct);
            }
        }

        return cart!;
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<CartDto>> GetCartAsync(string? userId, string? sessionId, CancellationToken ct = default)
    {
        var cart = await GetOrInitializeCartAsync(userId, sessionId, ct);
        return ApiResponse<CartDto>.Ok(_mapper.Map<CartDto>(cart));
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<CartDto>> AddToCartAsync(string? userId, string? sessionId, AddToCartRequest request, CancellationToken ct = default)
    {
        var cart = await GetOrInitializeCartAsync(userId, sessionId, ct);
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, ct);

        if (product is null || !product.IsActive || product.IsDeleted)
            return ApiResponse<CartDto>.Fail(404, "المنتج غير متاح", "Product unavailable.");

        if (product.StockQuantity < request.Quantity)
            return ApiResponse<CartDto>.Fail(400, "الكمية المطلوبة غير متوفرة في المخزون", "Insufficient stock.");

        cart.AddItem(request.ProductId, product.Price, request.Quantity);
        
        await _unitOfWork.SaveChangesAsync(ct);
        
        // Reload cart to get product mapping relations properly
        cart = await GetOrInitializeCartAsync(userId, sessionId, ct);
        
        return ApiResponse<CartDto>.Ok(_mapper.Map<CartDto>(cart), "تمت الإضافة للسلة", "Added to cart.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<CartDto>> UpdateItemQuantityAsync(string? userId, string? sessionId, int productId, UpdateCartItemRequest request, CancellationToken ct = default)
    {
        var cart = await GetOrInitializeCartAsync(userId, sessionId, ct);
        
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
            return ApiResponse<CartDto>.Fail(404, "المنتج غير موجود في السلة", "Item not in cart.");

        // Need eager load of product to check stock, but Specification loaded it already!
        if (item.Product.StockQuantity < request.Quantity)
            return ApiResponse<CartDto>.Fail(400, "الكمية المطلوبة غير متوفرة في المخزون", "Insufficient stock.");

        item.UpdateQuantity(request.Quantity);

        await _unitOfWork.SaveChangesAsync(ct);
        
        return ApiResponse<CartDto>.Ok(_mapper.Map<CartDto>(cart), "الكمية محدثة", "Quantity updated.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<CartDto>> RemoveItemAsync(string? userId, string? sessionId, int productId, CancellationToken ct = default)
    {
        var cart = await GetOrInitializeCartAsync(userId, sessionId, ct);
        
        cart.RemoveItem(productId);
        
        await _unitOfWork.SaveChangesAsync(ct);
        return ApiResponse<CartDto>.Ok(_mapper.Map<CartDto>(cart), "تم الحذف من السلة", "Removed from cart.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> ClearCartAsync(string? userId, string? sessionId, CancellationToken ct = default)
    {
        var cart = await GetOrInitializeCartAsync(userId, sessionId, ct);
        cart.Clear();
        await _unitOfWork.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "تم إفراغ السلة", "Cart cleared.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> MergeCartAsync(string userId, string sessionId, CancellationToken ct = default)
    {
        var guestSpec = new CartWithItemsSpecification(sessionId, 0);
        var guestCart = await _unitOfWork.Carts.GetEntityWithSpecAsync(guestSpec, ct);

        if (guestCart is null || guestCart.IsEmpty)
            return ApiResponse<bool>.Ok(true); // Nothing to merge

        var userSpec = new CartWithItemsSpecification(userId, true);
        var userCart = await _unitOfWork.Carts.GetEntityWithSpecAsync(userSpec, ct);

        if (userCart is null)
        {
            // User had no cart, simply assign guest cart to user
            guestCart.AssignToUser(userId);
        }
        else
        {
            // Merge items into user cart
            foreach (var item in guestCart.Items.ToList())
            {
                userCart.AddItem(item.ProductId, item.UnitPrice, item.Quantity);
            }
            // Delete guest cart entirely
            _unitOfWork.Carts.Remove(guestCart);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "تم دمج السلة", "Cart merged.");
    }
}

