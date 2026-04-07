namespace GalleryBetak.Domain.Entities;

/// <summary>
/// Product category with self-referencing hierarchy (max 3 levels).
/// Root → Sub → Sub-sub.
/// </summary>
public sealed class Category : BaseEntity
{
    /// <summary>Category name in Arabic.</summary>
    public string NameAr { get; private set; } = string.Empty;

    /// <summary>Category name in English.</summary>
    public string NameEn { get; private set; } = string.Empty;

    /// <summary>URL-friendly slug (unique, lowercase, hyphenated).</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Description in Arabic.</summary>
    public string? DescriptionAr { get; private set; }

    /// <summary>Description in English.</summary>
    public string? DescriptionEn { get; private set; }

    /// <summary>Category image URL.</summary>
    public string? ImageUrl { get; private set; }

    /// <summary>Parent category ID (null for root categories).</summary>
    public int? ParentId { get; private set; }

    /// <summary>Display order for UI sorting.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Whether the category is visible to customers.</summary>
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    /// <summary>Parent category (null for root).</summary>
    public Category? Parent { get; private set; }

    /// <summary>Child subcategories.</summary>
    public ICollection<Category> Children { get; private set; } = new List<Category>();

    /// <summary>Products in this category.</summary>
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    // Private constructor for EF
    private Category() { }

    /// <summary>
    /// Factory method to create a category.
    /// </summary>
    public static Category Create(string nameAr, string nameEn, string slug, string? descriptionAr, string? descriptionEn, int? parentId, string? imageUrl, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new Exceptions.DomainException("اسم التصنيف بالعربية مطلوب", "Category Arabic name is required.");
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new Exceptions.DomainException("اسم التصنيف بالإنجليزية مطلوب", "Category English name is required.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new Exceptions.DomainException("الرابط المختصر مطلوب", "Slug is required.");

        return new Category
        {
            NameAr = nameAr,
            NameEn = nameEn,
            Slug = slug.ToLowerInvariant(),
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            ImageUrl = imageUrl,
            ParentId = parentId,
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    /// <summary>Updates category details.</summary>
    public void Update(string nameAr, string nameEn, string slug, string? descriptionAr, string? descriptionEn, string? imageUrl, int displayOrder)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        Slug = slug.ToLowerInvariant();
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        ImageUrl = imageUrl;
        DisplayOrder = displayOrder;
    }

    /// <summary>Activates the category.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Deactivates the category.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Changes the parent category.</summary>
    public void SetParent(int? parentId) => ParentId = parentId;
}

