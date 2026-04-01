namespace ElMasria.Application.DTOs.Product;

/// <summary>Product list item DTO (used in catalog/search results).</summary>
public sealed record ProductListDto
{
    /// <summary>Product ID.</summary>
    public int Id { get; init; }

    /// <summary>Arabic name.</summary>
    public string NameAr { get; init; } = string.Empty;

    /// <summary>English name.</summary>
    public string NameEn { get; init; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>Current price in EGP.</summary>
    public decimal Price { get; init; }

    /// <summary>Original price before discount.</summary>
    public decimal? OriginalPrice { get; init; }

    /// <summary>Discount percentage (computed).</summary>
    public int? DiscountPercentage { get; init; }

    /// <summary>Primary image URL.</summary>
    public string? PrimaryImageUrl { get; init; }

    /// <summary>Average rating (0-5).</summary>
    public decimal AverageRating { get; init; }

    /// <summary>Total review count.</summary>
    public int ReviewCount { get; init; }

    /// <summary>Whether in stock.</summary>
    public bool InStock { get; init; }

    /// <summary>Whether product is featured.</summary>
    public bool IsFeatured { get; init; }

    /// <summary>Category name (Arabic).</summary>
    public string? CategoryNameAr { get; init; }

    /// <summary>Category name (English).</summary>
    public string? CategoryNameEn { get; init; }
}

/// <summary>Full product detail DTO (product page).</summary>
public sealed record ProductDetailDto
{
    /// <summary>Product ID.</summary>
    public int Id { get; init; }

    /// <summary>Arabic name.</summary>
    public string NameAr { get; init; } = string.Empty;

    /// <summary>English name.</summary>
    public string NameEn { get; init; } = string.Empty;

    /// <summary>URL slug.</summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>Arabic description.</summary>
    public string? DescriptionAr { get; init; }

    /// <summary>English description.</summary>
    public string? DescriptionEn { get; init; }

    /// <summary>Current price in EGP.</summary>
    public decimal Price { get; init; }

    /// <summary>Original price before discount.</summary>
    public decimal? OriginalPrice { get; init; }

    /// <summary>Discount percentage.</summary>
    public int? DiscountPercentage { get; init; }

    /// <summary>SKU code.</summary>
    public string SKU { get; init; } = string.Empty;

    /// <summary>Stock quantity.</summary>
    public int StockQuantity { get; init; }

    /// <summary>Whether in stock.</summary>
    public bool InStock { get; init; }

    /// <summary>Weight in kg.</summary>
    public decimal? Weight { get; init; }

    /// <summary>Dimensions (L×W×H).</summary>
    public string? Dimensions { get; init; }

    /// <summary>Material.</summary>
    public string? Material { get; init; }

    /// <summary>Country of origin.</summary>
    public string? Origin { get; init; }

    /// <summary>Whether featured.</summary>
    public bool IsFeatured { get; init; }

    /// <summary>Average rating.</summary>
    public decimal AverageRating { get; init; }

    /// <summary>Total reviews.</summary>
    public int ReviewCount { get; init; }

    /// <summary>Total views.</summary>
    public int ViewCount { get; init; }

    /// <summary>Category info.</summary>
    public ProductCategoryDto? Category { get; init; }

    /// <summary>Product images.</summary>
    public IReadOnlyList<ProductImageDto> Images { get; init; } = [];

    /// <summary>Tags.</summary>
    public IReadOnlyList<ProductTagDto> Tags { get; init; } = [];

    /// <summary>Creation date.</summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>Product image DTO.</summary>
public sealed record ProductImageDto
{
    /// <summary>Image ID.</summary>
    public int Id { get; init; }
    /// <summary>Full image URL.</summary>
    public string ImageUrl { get; init; } = string.Empty;
    /// <summary>Thumbnail URL.</summary>
    public string? ThumbnailUrl { get; init; }
    /// <summary>Arabic alt text.</summary>
    public string? AltTextAr { get; init; }
    /// <summary>English alt text.</summary>
    public string? AltTextEn { get; init; }
    /// <summary>Whether primary image.</summary>
    public bool IsPrimary { get; init; }
    /// <summary>Display order.</summary>
    public int DisplayOrder { get; init; }
}

/// <summary>Nested category DTO for product detail.</summary>
public sealed record ProductCategoryDto
{
    /// <summary>Category ID.</summary>
    public int Id { get; init; }
    /// <summary>Arabic name.</summary>
    public string NameAr { get; init; } = string.Empty;
    /// <summary>English name.</summary>
    public string NameEn { get; init; } = string.Empty;
    /// <summary>Slug.</summary>
    public string Slug { get; init; } = string.Empty;
}

/// <summary>Tag DTO for product detail.</summary>
public sealed record ProductTagDto
{
    /// <summary>Tag ID.</summary>
    public int Id { get; init; }
    /// <summary>Arabic name.</summary>
    public string NameAr { get; init; } = string.Empty;
    /// <summary>English name.</summary>
    public string NameEn { get; init; } = string.Empty;
}

/// <summary>Create product request DTO (admin).</summary>
public sealed record CreateProductRequest
{
    /// <summary>Arabic name.</summary>
    public string NameAr { get; init; } = string.Empty;
    /// <summary>English name.</summary>
    public string NameEn { get; init; } = string.Empty;
    /// <summary>Arabic description.</summary>
    public string? DescriptionAr { get; init; }
    /// <summary>English description.</summary>
    public string? DescriptionEn { get; init; }
    /// <summary>Price in EGP.</summary>
    public decimal Price { get; init; }
    /// <summary>Original price (before discount).</summary>
    public decimal? OriginalPrice { get; init; }
    /// <summary>SKU code.</summary>
    public string SKU { get; init; } = string.Empty;
    /// <summary>Initial stock quantity.</summary>
    public int StockQuantity { get; init; }
    /// <summary>Category ID.</summary>
    public int CategoryId { get; init; }
    /// <summary>Weight in kg.</summary>
    public decimal? Weight { get; init; }
    /// <summary>Dimensions string.</summary>
    public string? Dimensions { get; init; }
    /// <summary>Material.</summary>
    public string? Material { get; init; }
    /// <summary>Country of origin.</summary>
    public string? Origin { get; init; }
    /// <summary>Whether featured.</summary>
    public bool IsFeatured { get; init; }
    /// <summary>Tag IDs to attach.</summary>
    public List<int> TagIds { get; init; } = [];
}

/// <summary>Update product request DTO (admin).</summary>
public sealed record UpdateProductRequest
{
    /// <summary>Arabic name.</summary>
    public string NameAr { get; init; } = string.Empty;
    /// <summary>English name.</summary>
    public string NameEn { get; init; } = string.Empty;
    /// <summary>Arabic description.</summary>
    public string? DescriptionAr { get; init; }
    /// <summary>English description.</summary>
    public string? DescriptionEn { get; init; }
    /// <summary>Price in EGP.</summary>
    public decimal Price { get; init; }
    /// <summary>Original price.</summary>
    public decimal? OriginalPrice { get; init; }
    /// <summary>Stock quantity.</summary>
    public int StockQuantity { get; init; }
    /// <summary>Category ID.</summary>
    public int CategoryId { get; init; }
    /// <summary>Weight.</summary>
    public decimal? Weight { get; init; }
    /// <summary>Dimensions string.</summary>
    public string? Dimensions { get; init; }
    /// <summary>Material.</summary>
    public string? Material { get; init; }
    /// <summary>Country of origin.</summary>
    public string? Origin { get; init; }
    /// <summary>Whether featured.</summary>
    public bool IsFeatured { get; init; }
    /// <summary>Whether active.</summary>
    public bool IsActive { get; init; } = true;
    /// <summary>Tag IDs to replace.</summary>
    public List<int> TagIds { get; init; } = [];
}

/// <summary>Product query parameters for filtering/pagination.</summary>
public sealed record ProductQueryParams
{
    /// <summary>Search term (bilingual).</summary>
    public string? Search { get; init; }
    /// <summary>Filter by category ID.</summary>
    public int? CategoryId { get; init; }
    /// <summary>Filter by tag ID.</summary>
    public int? TagId { get; init; }
    /// <summary>Minimum price filter.</summary>
    public decimal? MinPrice { get; init; }
    /// <summary>Maximum price filter.</summary>
    public decimal? MaxPrice { get; init; }
    /// <summary>Only featured products.</summary>
    public bool? IsFeatured { get; init; }
    /// <summary>Only in-stock products.</summary>
    public bool? InStock { get; init; }
    /// <summary>Sort by field (price, name, rating, newest).</summary>
    public string? SortBy { get; init; }
    /// <summary>Sort direction (asc/desc).</summary>
    public string? SortDirection { get; init; }
    /// <summary>Page number (1-indexed).</summary>
    public int PageNumber { get; init; } = 1;
    /// <summary>Page size.</summary>
    public int PageSize { get; init; } = 12;
}
