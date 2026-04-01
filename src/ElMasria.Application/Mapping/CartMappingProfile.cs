using AutoMapper;
using ElMasria.Application.DTOs.Cart;
using DomainCart = ElMasria.Domain.Entities.Cart;
using DomainCartItem = ElMasria.Domain.Entities.CartItem;

namespace ElMasria.Application.Mapping;

/// <summary>
/// AutoMapper profile for Cart configurations.
/// </summary>
public sealed class CartMappingProfile : Profile
{
    public CartMappingProfile()
    {
        CreateMap<DomainCart, CartDto>();

        CreateMap<DomainCartItem, CartItemDto>()
            .ForMember(d => d.ProductNameAr, opt => opt.MapFrom(s => s.Product.NameAr))
            .ForMember(d => d.ProductNameEn, opt => opt.MapFrom(s => s.Product.NameEn))
            .ForMember(d => d.StockQuantity, opt => opt.MapFrom(s => s.Product.StockQuantity))
            .ForMember(d => d.ImageUrl, opt => opt.MapFrom(s => 
                s.Product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault()));
    }
}
