namespace GalleryBetak.Application.DTOs.Category;

/// <summary>Standard category list DTO, including child categories if applicable.</summary>
public sealed record CategoryDto
{
    /// <summary>Category ID.</summary>
    public int Id { get; init; }

    /// <summary>Arabic name.</summary>
    public string NameAr { get; init; } = string.Empty;

    /// <summary>English name.</summary>
    public string NameEn { get; init; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>Image URL for the category banner/icon.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Display order.</summary>
    public int DisplayOrder { get; init; }

    /// <summary>Child categories (subcategories).</summary>
    public IReadOnlyList<CategoryDto> SubCategories { get; init; } = [];
}

/// <summary>Category representation used for breadcrumbs path.</summary>
public sealed record CategoryBreadcrumbDto
{
    /// <summary>Category ID.</summary>
    public int Id { get; init; }

    /// <summary>Arabic name.</summary>
    public string NameAr { get; init; } = string.Empty;

    /// <summary>English name.</summary>
    public string NameEn { get; init; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; init; } = string.Empty;
}

/// <summary>Detailed category DTO, including full breadcrumb path.</summary>
public sealed record CategoryDetailDto
{
    /// <summary>Category ID.</summary>
    public int Id { get; init; }

    /// <summary>Arabic name.</summary>
    public string NameAr { get; init; } = string.Empty;

    /// <summary>English name.</summary>
    public string NameEn { get; init; } = string.Empty;

    /// <summary>Arabic description.</summary>
    public string? DescriptionAr { get; init; }

    /// <summary>English description.</summary>
    public string? DescriptionEn { get; init; }

    /// <summary>URL slug.</summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>Image URL.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Parent category ID if any.</summary>
    public int? ParentId { get; init; }

    /// <summary>Hierarchical breadcrumb path (root -> ... -> current).</summary>
    public IReadOnlyList<CategoryBreadcrumbDto> Breadcrumbs { get; init; } = [];

    /// <summary>Immediate subcategories at this level.</summary>
    public IReadOnlyList<CategoryDto> SubCategories { get; init; } = [];
}

/// <summary>Request DTO for creating a category (admin).</summary>
public sealed record CreateCategoryRequest
{
    /// <summary>Arabic name.</summary>
    public string NameAr { get; init; } = string.Empty;

    /// <summary>English name.</summary>
    public string NameEn { get; init; } = string.Empty;

    /// <summary>Arabic description.</summary>
    public string? DescriptionAr { get; init; }

    /// <summary>English description.</summary>
    public string? DescriptionEn { get; init; }

    /// <summary>Optional parent category ID to create a subcategory.</summary>
    public int? ParentId { get; init; }

    /// <summary>Image URL (or handle upload separately).</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Ordering priority.</summary>
    public int DisplayOrder { get; init; }
}

/// <summary>Request DTO for updating a category (admin).</summary>
public sealed record UpdateCategoryRequest
{
    /// <summary>Arabic name.</summary>
    public string NameAr { get; init; } = string.Empty;

    /// <summary>English name.</summary>
    public string NameEn { get; init; } = string.Empty;

    /// <summary>Arabic description.</summary>
    public string? DescriptionAr { get; init; }

    /// <summary>English description.</summary>
    public string? DescriptionEn { get; init; }

    /// <summary>Optional parent category ID.</summary>
    public int? ParentId { get; init; }

    /// <summary>Image URL.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Ordering priority.</summary>
    public int DisplayOrder { get; init; }

    /// <summary>Whether active or hidden.</summary>
    public bool IsActive { get; init; } = true;
}

