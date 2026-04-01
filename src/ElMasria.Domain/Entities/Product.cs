using ElMasria.Domain.Enums;
using ElMasria.Domain.Events;
using ElMasria.Domain.Exceptions;

namespace ElMasria.Domain.Entities;

/// <summary>
/// Core product entity with DDD patterns: private setters, domain methods,
/// stock management, rating calculation, and event raising.
/// </summary>
public sealed class Product : BaseEntity
{
    /// <summary>Product name in Arabic (primary).</summary>
    public string NameAr { get; private set; } = string.Empty;

    /// <summary>Product name in English.</summary>
    public string NameEn { get; private set; } = string.Empty;

    /// <summary>SEO-friendly URL slug (unique).</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Detailed description in Arabic.</summary>
    public string? DescriptionAr { get; private set; }

    /// <summary>Detailed description in English.</summary>
    public string? DescriptionEn { get; private set; }

    /// <summary>Current selling price in EGP. Must be > 0.</summary>
    public decimal Price { get; private set; }

    /// <summary>Original price before discount (null if no discount).</summary>
    public decimal? OriginalPrice { get; private set; }

    /// <summary>Stock Keeping Unit (unique per product).</summary>
    public string SKU { get; private set; } = string.Empty;

    /// <summary>Current stock quantity. Must be >= 0.</summary>
    public int StockQuantity { get; private set; }

    /// <summary>Category this product belongs to.</summary>
    public int CategoryId { get; private set; }

    /// <summary>Whether featured on homepage.</summary>
    public bool IsFeatured { get; private set; }

    /// <summary>Whether visible to customers.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Product weight in kg.</summary>
    public decimal? Weight { get; private set; }

    /// <summary>Dimensions (e.g., "30x40x10 سم").</summary>
    public string? Dimensions { get; private set; }

    /// <summary>Material composition.</summary>
    public string? Material { get; private set; }

    /// <summary>Country/region of origin.</summary>
    public string? Origin { get; private set; }

    /// <summary>Calculated average rating (0.00 to 5.00).</summary>
    public decimal AverageRating { get; private set; }

    /// <summary>Total number of approved reviews.</summary>
    public int ReviewCount { get; private set; }

    /// <summary>Total page view count.</summary>
    public int ViewCount { get; private set; }

    // ── Domain Events ─────────────────────────────────────────────
    private readonly List<BaseDomainEvent> _domainEvents = new();

    /// <summary>Domain events raised by this entity.</summary>
    public IReadOnlyList<BaseDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Clears all domain events (called by DbContext after dispatch).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    // ── Navigation Properties ─────────────────────────────────────
    /// <summary>Parent category.</summary>
    public Category Category { get; private set; } = null!;

    /// <summary>Product images.</summary>
    public ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();

    /// <summary>Product tags (M:N).</summary>
    public ICollection<ProductTag> ProductTags { get; private set; } = new List<ProductTag>();

    /// <summary>Product reviews.</summary>
    public ICollection<Review> Reviews { get; private set; } = new List<Review>();

    // Private constructor for EF
    private Product() { }

    /// <summary>
    /// Factory method to create a product with validated input.
    /// </summary>
    public static Product Create(
        string nameAr, string nameEn, string slug, string sku,
        decimal price, int categoryId, int stockQuantity = 0)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("اسم المنتج بالعربية مطلوب", "Product Arabic name is required.");
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("كود المنتج مطلوب", "SKU is required.");
        if (price <= 0)
            throw new DomainException("السعر يجب أن يكون أكبر من صفر", "Price must be greater than zero.");
        if (stockQuantity < 0)
            throw new DomainException("الكمية لا يمكن أن تكون أقل من صفر", "Stock quantity cannot be negative.");

        return new Product
        {
            NameAr = nameAr,
            NameEn = nameEn,
            Slug = slug.ToLowerInvariant(),
            SKU = sku.ToUpperInvariant(),
            Price = price,
            CategoryId = categoryId,
            StockQuantity = stockQuantity,
            IsActive = true
        };
    }

    /// <summary>
    /// Factory overload that auto-generates slug from English name.
    /// </summary>
    public static Product Create(
        string nameAr, string nameEn,
        string? descriptionAr, string? descriptionEn,
        decimal price, string sku, int stockQuantity, int categoryId)
    {
        var slug = GenerateSlug(nameEn);
        var product = Create(nameAr, nameEn, slug, sku, price, categoryId, stockQuantity);
        product.DescriptionAr = descriptionAr;
        product.DescriptionEn = descriptionEn;
        return product;
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("&", "and");
    }

    /// <summary>Updates product details.</summary>
    public void Update(
        string nameAr, string nameEn, string slug,
        string? descriptionAr, string? descriptionEn,
        decimal price, decimal? originalPrice,
        string? material, string? origin, string? dimensions, decimal? weight,
        int categoryId)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        Slug = slug.ToLowerInvariant();
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        Price = price;
        OriginalPrice = originalPrice;
        Material = material;
        Origin = origin;
        Dimensions = dimensions;
        Weight = weight;
        CategoryId = categoryId;
    }

    /// <summary>Sets the selling price. Must be > 0.</summary>
    public void SetPrice(decimal price)
    {
        if (price <= 0)
            throw new DomainException("السعر يجب أن يكون أكبر من صفر", "Price must be greater than zero.");
        Price = price;
    }

    /// <summary>Sets a discount by providing the original (before-discount) price.</summary>
    public void SetDiscount(decimal originalPrice)
    {
        if (originalPrice <= Price)
            throw new DomainException("السعر الأصلي يجب أن يكون أكبر من سعر البيع", "Original price must be greater than selling price.");
        OriginalPrice = originalPrice;
    }

    /// <summary>Removes any active discount.</summary>
    public void RemoveDiscount() => OriginalPrice = null;

    /// <summary>
    /// Deducts stock for an order. Raises LowStockEvent if below threshold.
    /// </summary>
    /// <param name="quantity">Quantity to deduct.</param>
    /// <exception cref="InsufficientStockException">When requested quantity exceeds available stock.</exception>
    public void DeductStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من صفر", "Quantity must be greater than zero.");
        if (quantity > StockQuantity)
            throw new InsufficientStockException(StockQuantity, quantity);

        StockQuantity -= quantity;

        // Raise low stock alert if below threshold
        if (StockQuantity < 5)
        {
            _domainEvents.Add(new ProductStockLowEvent(Id, NameAr, StockQuantity));
        }
    }

    /// <summary>Adds stock (restock or return).</summary>
    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من صفر", "Quantity must be greater than zero.");
        StockQuantity += quantity;
    }

    /// <summary>Recalculates average rating from approved reviews.</summary>
    public void RecalculateRating(decimal averageRating, int reviewCount)
    {
        AverageRating = Math.Round(Math.Clamp(averageRating, 0m, 5m), 2);
        ReviewCount = Math.Max(0, reviewCount);
    }

    /// <summary>Increments the page view counter.</summary>
    public void IncrementViewCount() => ViewCount++;

    /// <summary>Marks product as featured.</summary>
    public void SetFeatured(bool isFeatured) => IsFeatured = isFeatured;

    /// <summary>Sets active/inactive status.</summary>
    public void SetActive(bool isActive) => IsActive = isActive;

    /// <summary>Activates the product (visible to customers).</summary>
    public void Activate() => IsActive = true;

    /// <summary>Deactivates the product (hidden from customers).</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Updates the price and optional original price.</summary>
    public void UpdatePrice(decimal price, decimal? originalPrice = null)
    {
        if (price <= 0)
            throw new DomainException("السعر يجب أن يكون أكبر من صفر", "Price must be greater than zero.");
        Price = price;
        OriginalPrice = originalPrice;
    }

    /// <summary>Sets stock quantity directly (admin recount).</summary>
    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new DomainException("الكمية لا يمكن أن تكون سالبة", "Stock cannot be negative.");
        StockQuantity = quantity;
        if (StockQuantity < 5)
            _domainEvents.Add(new ProductStockLowEvent(Id, NameAr, StockQuantity));
    }

    /// <summary>Adds a tag association to this product.</summary>
    public void AddTag(int tagId)
    {
        if (ProductTags.Any(pt => pt.TagId == tagId)) return;
        ProductTags.Add(ProductTag.Create(Id, tagId));
    }

    /// <summary>Removes all tag associations.</summary>
    public void ClearTags() => ProductTags.Clear();

    /// <summary>Updates basic product info (simplified overload for service layer).</summary>
    public void Update(string nameAr, string nameEn,
        string? descriptionAr, string? descriptionEn, int categoryId)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        Slug = GenerateSlug(nameEn);
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        CategoryId = categoryId;
    }

    /// <summary>Whether the product has a discount.</summary>
    public bool HasDiscount => OriginalPrice.HasValue && OriginalPrice > Price;

    /// <summary>Discount percentage (0 if no discount).</summary>
    public int DiscountPercentage => HasDiscount
        ? (int)Math.Round((1 - Price / OriginalPrice!.Value) * 100)
        : 0;

    /// <summary>Whether the product is in stock.</summary>
    public bool IsInStock => StockQuantity > 0;
}
