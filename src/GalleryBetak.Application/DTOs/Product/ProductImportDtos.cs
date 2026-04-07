namespace GalleryBetak.Application.DTOs.Product;

/// <summary>Admin request payload for importing a product from an external URL.</summary>
public sealed record ProductImportRequest
{
    /// <summary>Public product page URL to import from.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Optional category selected by admin as a preferred target.</summary>
    public int? PreferredCategoryId { get; init; }
}

/// <summary>Preview data extracted from an external product page.</summary>
public sealed record ProductImportPreviewDto
{
    /// <summary>The source URL that was imported.</summary>
    public string SourceUrl { get; init; } = string.Empty;

    /// <summary>Source host/domain name.</summary>
    public string SourceHost { get; init; } = string.Empty;

    /// <summary>Suggested Arabic name (editable by admin).</summary>
    public string NameAr { get; init; } = string.Empty;

    /// <summary>Suggested English name (editable by admin).</summary>
    public string NameEn { get; init; } = string.Empty;

    /// <summary>Suggested Arabic description.</summary>
    public string? DescriptionAr { get; init; }

    /// <summary>Suggested English description.</summary>
    public string? DescriptionEn { get; init; }

    /// <summary>Suggested product price.</summary>
    public decimal Price { get; init; }

    /// <summary>Suggested original price before discount.</summary>
    public decimal? OriginalPrice { get; init; }

    /// <summary>Suggested SKU for admin review.</summary>
    public string SuggestedSku { get; init; } = string.Empty;

    /// <summary>Suggested category ID based on admin preference or source hints.</summary>
    public int? SuggestedCategoryId { get; init; }

    /// <summary>Extracted product weight.</summary>
    public decimal? Weight { get; init; }

    /// <summary>Extracted product dimensions.</summary>
    public string? Dimensions { get; init; }

    /// <summary>Extracted material.</summary>
    public string? Material { get; init; }

    /// <summary>Extracted country/region of origin.</summary>
    public string? Origin { get; init; }

    /// <summary>Source currency code if detected.</summary>
    public string? Currency { get; init; }

    /// <summary>Uploaded image URLs prepared for product creation.</summary>
    public IReadOnlyList<string> ImageUrls { get; init; } = [];

    /// <summary>Non-blocking warnings generated during import.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
