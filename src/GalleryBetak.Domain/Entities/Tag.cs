namespace GalleryBetak.Domain.Entities;

/// <summary>
/// Product tag for filtering and categorization (e.g., "جديد", "الأكثر طلباً").
/// </summary>
public sealed class Tag : BaseEntity
{
    /// <summary>Tag name in Arabic.</summary>
    public string NameAr { get; private set; } = string.Empty;

    /// <summary>Tag name in English.</summary>
    public string NameEn { get; private set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; private set; } = string.Empty;

    // Navigation
    /// <summary>Products with this tag (M:N via ProductTag).</summary>
    public ICollection<ProductTag> ProductTags { get; private set; } = new List<ProductTag>();

    private Tag() { }

    /// <summary>Creates a new tag.</summary>
    public static Tag Create(string nameAr, string nameEn, string slug)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new Exceptions.DomainException("اسم التاق بالعربية مطلوب", "Tag Arabic name is required.");

        return new Tag
        {
            NameAr = nameAr,
            NameEn = nameEn,
            Slug = slug.ToLowerInvariant()
        };
    }

    /// <summary>Updates tag details.</summary>
    public void Update(string nameAr, string nameEn, string slug)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        Slug = slug.ToLowerInvariant();
    }
}

/// <summary>Junction entity for Product ↔ Tag many-to-many relationship.</summary>
public sealed class ProductTag
{
    /// <summary>Product ID.</summary>
    public int ProductId { get; private set; }

    /// <summary>Tag ID.</summary>
    public int TagId { get; private set; }

    // Navigation
    /// <summary>The product.</summary>
    public Product Product { get; private set; } = null!;

    /// <summary>The tag.</summary>
    public Tag Tag { get; private set; } = null!;

    private ProductTag() { }

    /// <summary>Creates a product-tag association.</summary>
    public static ProductTag Create(int productId, int tagId) => new()
    {
        ProductId = productId,
        TagId = tagId
    };
}

