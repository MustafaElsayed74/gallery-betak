using ElMasria.Domain.Entities;
using ElMasria.Domain.Specifications;

namespace ElMasria.Application.Specifications;

/// <summary>
/// Specification to count products matching the exact same filters as the paginated query.
/// Used for total items calculation in pagination.
/// </summary>
public class ProductFiltersForCountSpecification : BaseSpecification<Product>
{
    public ProductFiltersForCountSpecification(
        string? search,
        int? categoryId,
        bool activeOnly = true)
        : base(x =>
            (!activeOnly || x.IsActive) &&
            (!categoryId.HasValue || x.CategoryId == categoryId) &&
            (string.IsNullOrEmpty(search) || x.NameAr.Contains(search) || x.NameEn.Contains(search) || x.DescriptionAr!.Contains(search)))
    {
    }
}
