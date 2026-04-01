using AutoMapper;
using ElMasria.Application.DTOs.Product;
using ElMasria.Domain.Entities;

namespace ElMasria.Application.Mapping;

/// <summary>
/// AutoMapper profile for Product entity ↔ DTO mappings.
/// </summary>
public sealed class ProductMappingProfile : Profile
{
    /// <summary>Configures product mappings.</summary>
    public ProductMappingProfile()
    {
        // Product → ProductListDto
        CreateMap<Product, ProductListDto>()
            .ForMember(d => d.PrimaryImageUrl, opt =>
                opt.MapFrom(s => s.Images
                    .Where(i => i.IsPrimary)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()
                    ?? s.Images
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()))
            .ForMember(d => d.InStock, opt =>
                opt.MapFrom(s => s.StockQuantity > 0))
            .ForMember(d => d.DiscountPercentage, opt =>
                opt.MapFrom(s => s.OriginalPrice.HasValue && s.OriginalPrice > 0
                    ? (int)Math.Round((1 - s.Price / s.OriginalPrice.Value) * 100)
                    : (int?)null))
            .ForMember(d => d.CategoryNameAr, opt =>
                opt.MapFrom(s => s.Category != null ? s.Category.NameAr : null))
            .ForMember(d => d.CategoryNameEn, opt =>
                opt.MapFrom(s => s.Category != null ? s.Category.NameEn : null));

        // Product → ProductDetailDto
        CreateMap<Product, ProductDetailDto>()
            .ForMember(d => d.InStock, opt =>
                opt.MapFrom(s => s.StockQuantity > 0))
            .ForMember(d => d.DiscountPercentage, opt =>
                opt.MapFrom(s => s.OriginalPrice.HasValue && s.OriginalPrice > 0
                    ? (int)Math.Round((1 - s.Price / s.OriginalPrice.Value) * 100)
                    : (int?)null))
            .ForMember(d => d.Tags, opt =>
                opt.MapFrom(s => s.ProductTags.Select(pt => pt.Tag)));

        // ProductImage → ProductImageDto
        CreateMap<ProductImage, ProductImageDto>();

        // Category → ProductCategoryDto
        CreateMap<Category, ProductCategoryDto>();

        // Tag → ProductTagDto
        CreateMap<Tag, ProductTagDto>();
    }
}
