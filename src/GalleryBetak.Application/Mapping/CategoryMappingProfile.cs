using AutoMapper;
using GalleryBetak.Application.DTOs.Category;
using DomainCategory = GalleryBetak.Domain.Entities.Category; // Alias to prevent namespace conflict

namespace GalleryBetak.Application.Mapping;

/// <summary>
/// AutoMapper profile for Category entity ↔ DTO mappings.
/// </summary>
public sealed class CategoryMappingProfile : Profile
{
    /// <summary>Configures category mappings.</summary>
    public CategoryMappingProfile()
    {
        // Category -> CategoryDto with hierarchical subcategories
        CreateMap<DomainCategory, CategoryDto>()
            .ForMember(d => d.SubCategories, opt => opt.MapFrom(s => s.Children.Where(sc => !sc.IsDeleted && sc.IsActive).OrderBy(sc => sc.DisplayOrder)));

        // Category -> CategoryDetailDto (breadcrumbs mapped manually in service layer as it requires recursive querying)
        CreateMap<DomainCategory, CategoryDetailDto>()
            .ForMember(d => d.SubCategories, opt => opt.MapFrom(s => s.Children.Where(sc => !sc.IsDeleted && sc.IsActive).OrderBy(sc => sc.DisplayOrder)))
            .ForMember(d => d.Breadcrumbs, opt => opt.Ignore());

        // Category -> CategoryBreadcrumbDto
        CreateMap<DomainCategory, CategoryBreadcrumbDto>();
    }
}

