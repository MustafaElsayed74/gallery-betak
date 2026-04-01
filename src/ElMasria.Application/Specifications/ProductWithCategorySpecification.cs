using ElMasria.Domain.Entities;
using ElMasria.Domain.Specifications;

namespace ElMasria.Application.Specifications;

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
                case "priceAsc":
                    ApplyOrderBy(p => p.Price);
                    break;
                case "priceDesc":
                    ApplyOrderByDescending(p => p.Price);
                    break;
                case "nameArAsc":
                    ApplyOrderBy(p => p.NameAr);
                    break;
                case "nameEnAsc":
                    ApplyOrderBy(p => p.NameEn);
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
