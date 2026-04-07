using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Specifications;

namespace GalleryBetak.Application.Specifications;

/// <summary>
/// Specification for querying products with optional filtering, sorting, and pagination.
/// Includes bilingual search and category filtering.
/// </summary>
public class ProductWithCategorySpecification : BaseSpecification<Product>
{
    public ProductWithCategorySpecification(
        string? search,
        int? categoryId,
        string? sort,
        int skip,
        int take,
        bool activeOnly = true)
        : base(x =>
            (!activeOnly || x.IsActive) &&
            (!categoryId.HasValue || x.CategoryId == categoryId) &&
            (string.IsNullOrEmpty(search) || x.NameAr.Contains(search) || x.NameEn.Contains(search) || x.DescriptionAr!.Contains(search)))
    {
        AddInclude(x => x.Category);
        AddInclude(x => x.Images);

        ApplyPaging(skip, take);

        if (!string.IsNullOrEmpty(sort))
        {
            switch (sort.ToLowerInvariant())
            {
                case "priceasc":
                    ApplyOrderBy(p => p.Price);
                    break;
                case "pricedesc":
                    ApplyOrderByDescending(p => p.Price);
                    break;
                case "namearasc":
                    ApplyOrderBy(p => p.NameAr);
                    break;
                case "nameenasc":
                    ApplyOrderBy(p => p.NameEn);
                    break;
                case "newest":
                    ApplyOrderByDescending(p => p.CreatedAt);
                    break;
                case "ratingasc":
                    ApplyOrderBy(p => p.AverageRating);
                    break;
                case "ratingdesc":
                    ApplyOrderByDescending(p => p.AverageRating);
                    break;
                case "viewsasc":
                    ApplyOrderBy(p => p.ViewCount);
                    break;
                case "viewsdesc":
                    ApplyOrderByDescending(p => p.ViewCount);
                    break;
                default:
                    ApplyOrderBy(p => p.NameAr); // Default sort
                    break;
            }
        }
        else
        {
            ApplyOrderBy(p => p.Id); // Default fallback sort
        }
    }

    public ProductWithCategorySpecification(int id) 
        : base(x => x.Id == id)
    {
        AddInclude(x => x.Category);
        AddInclude(x => x.Images);
        AddInclude(x => x.ProductTags);
    }

    public ProductWithCategorySpecification(string slug) 
        : base(x => x.Slug == slug)
    {
        AddInclude(x => x.Category);
        AddInclude(x => x.Images);
        AddInclude(x => x.ProductTags);
        AddInclude(x => x.Reviews);
    }
}

