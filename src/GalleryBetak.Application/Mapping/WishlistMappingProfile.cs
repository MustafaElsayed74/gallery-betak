using AutoMapper;
using GalleryBetak.Application.DTOs.Wishlist;
using DomainWishlist = GalleryBetak.Domain.Entities.Wishlist;
using DomainWishlistItem = GalleryBetak.Domain.Entities.WishlistItem;

namespace GalleryBetak.Application.Mapping;

/// <summary>
/// AutoMapper profile for Wishlist configurations.
/// </summary>
public sealed class WishlistMappingProfile : Profile
{
    public WishlistMappingProfile()
    {
        CreateMap<DomainWishlist, WishlistDto>();

        CreateMap<DomainWishlistItem, WishlistItemDto>()
            .ForMember(d => d.ProductNameAr, opt => opt.MapFrom(s => s.Product.NameAr))
            .ForMember(d => d.ProductNameEn, opt => opt.MapFrom(s => s.Product.NameEn))
            .ForMember(d => d.UnitPrice, opt => opt.MapFrom(s => s.Product.Price))
            .ForMember(d => d.StockQuantity, opt => opt.MapFrom(s => s.Product.StockQuantity))
            .ForMember(d => d.ImageUrl, opt => opt.MapFrom(s => 
                s.Product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault()));
    }
}

