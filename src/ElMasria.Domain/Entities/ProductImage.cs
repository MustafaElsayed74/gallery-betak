namespace ElMasria.Domain.Entities;

/// <summary>
/// Product image with thumbnail support and alt text for SEO.
/// </summary>
public sealed class ProductImage
{
    /// <summary>Primary key.</summary>
    public int Id { get; private set; }

    /// <summary>Owning product ID.</summary>
    public int ProductId { get; private set; }

    /// <summary>Full-size image URL.</summary>
    public string ImageUrl { get; private set; } = string.Empty;

    /// <summary>Thumbnail (300px) URL.</summary>
    public string? ThumbnailUrl { get; private set; }

    /// <summary>Alt text in Arabic (SEO).</summary>
    public string? AltTextAr { get; private set; }

    /// <summary>Alt text in English (SEO).</summary>
    public string? AltTextEn { get; private set; }

    /// <summary>Display order in image gallery.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Whether this is the primary (hero) image.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Upload timestamp.</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    /// <summary>Owning product.</summary>
    public Product Product { get; private set; } = null!;

    private ProductImage() { }

    /// <summary>Creates a product image.</summary>
    public static ProductImage Create(int productId, string imageUrl, string? thumbnailUrl,
        string? altTextAr, string? altTextEn, int displayOrder, bool isPrimary)
    {
        return new ProductImage
        {
            ProductId = productId,
            ImageUrl = imageUrl,
            ThumbnailUrl = thumbnailUrl,
            AltTextAr = altTextAr,
            AltTextEn = altTextEn,
            DisplayOrder = displayOrder,
            IsPrimary = isPrimary
        };
    }

    /// <summary>Sets this image as the primary thumbnail.</summary>
    public void SetAsPrimary() => IsPrimary = true;

    /// <summary>Removes primary designation.</summary>
    public void ClearPrimary() => IsPrimary = false;

    /// <summary>Updates display order.</summary>
    public void SetDisplayOrder(int order) => DisplayOrder = order;
}
