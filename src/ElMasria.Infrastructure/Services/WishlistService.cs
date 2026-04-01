using AutoMapper;
using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Wishlist;
using ElMasria.Application.Interfaces;
using ElMasria.Application.Specifications;
using ElMasria.Domain.Entities;
using ElMasria.Domain.Interfaces;

namespace ElMasria.Infrastructure.Services;

/// <summary>
/// Implementation of user wishlist logic.
/// </summary>
public sealed class WishlistService : IWishlistService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICartService _cartService;

    public WishlistService(IUnitOfWork unitOfWork, IMapper mapper, ICartService cartService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cartService = cartService;
    }

    private async Task<Wishlist> GetOrInitializeWishlistAsync(string userId, CancellationToken ct)
    {
        var spec = new WishlistWithItemsSpecification(userId);
        var wishlist = await _unitOfWork.Wishlists.GetEntityWithSpecAsync(spec, ct);

        if (wishlist is null)
        {
            wishlist = Wishlist.Create(userId);
            await _unitOfWork.Wishlists.AddAsync(wishlist, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return wishlist;
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<WishlistDto>> GetWishlistAsync(string userId, CancellationToken ct = default)
    {
        var wishlist = await GetOrInitializeWishlistAsync(userId, ct);
        return ApiResponse<WishlistDto>.Ok(_mapper.Map<WishlistDto>(wishlist));
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<WishlistDto>> ToggleWishlistAsync(string userId, int productId, CancellationToken ct = default)
    {
        var wishlist = await GetOrInitializeWishlistAsync(userId, ct);
        var product = await _unitOfWork.Products.GetByIdAsync(productId, ct);

        if (product is null || !product.IsActive || product.IsDeleted)
            return ApiResponse<WishlistDto>.Fail(404, "المنتج غير متاح", "Product unavailable.");

        string messageAr, messageEn;

        if (wishlist.ContainsProduct(productId))
        {
            wishlist.RemoveItem(productId);
            messageAr = "تم إزالة المنتج من المفضلة";
            messageEn = "Removed from wishlist.";
        }
        else
        {
            wishlist.AddItem(productId);
            messageAr = "تمت إضافة المنتج للمفضلة";
            messageEn = "Added to wishlist.";
        }

        await _unitOfWork.SaveChangesAsync(ct);
        
        // Reload to map nested product image URLs
        wishlist = await GetOrInitializeWishlistAsync(userId, ct);
        
        return ApiResponse<WishlistDto>.Ok(_mapper.Map<WishlistDto>(wishlist), messageAr, messageEn);
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> ClearWishlistAsync(string userId, CancellationToken ct = default)
    {
        var wishlist = await GetOrInitializeWishlistAsync(userId, ct);
        
        // Wishlist item cascade drops are handled by DB schema, just clearing the collection is fine
        foreach (var item in wishlist.Items.ToList())
        {
            wishlist.RemoveItem(item.ProductId);
        }
        
        await _unitOfWork.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "تم إفراغ المفضلة", "Wishlist cleared.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<WishlistDto>> MoveToCartAsync(string userId, int productId, string? sessionId = null, CancellationToken ct = default)
    {
        var wishlist = await GetOrInitializeWishlistAsync(userId, ct);
        
        if (!wishlist.ContainsProduct(productId))
            return ApiResponse<WishlistDto>.Fail(404, "المنتج غير موجود في المفضلة", "Product not in wishlist.");

        // First, attempt to add to cart
        var addToCartRequest = new Application.DTOs.Cart.AddToCartRequest { ProductId = productId, Quantity = 1 };
        var cartResponse = await _cartService.AddToCartAsync(userId, sessionId, addToCartRequest, ct);

        if (!cartResponse.Success)
        {
            // E.g. insufficient stock
            return ApiResponse<WishlistDto>.Fail(cartResponse.StatusCode, cartResponse.Message, cartResponse.MessageEn);
        }

        // If successful, remove from wishlist
        wishlist.RemoveItem(productId);
        await _unitOfWork.SaveChangesAsync(ct);

        // Reload to map correctly
        wishlist = await GetOrInitializeWishlistAsync(userId, ct);
        
        return ApiResponse<WishlistDto>.Ok(_mapper.Map<WishlistDto>(wishlist), "تم النقل للسلة بنجاح", "Moved to cart successfully.");
    }
}
