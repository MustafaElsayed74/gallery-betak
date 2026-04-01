namespace ElMasria.Domain.Entities;

/// <summary>
/// Per-user wishlist. Each user has exactly one wishlist (auto-created).
/// </summary>
public sealed class Wishlist
{
    /// <summary>Primary key.</summary>
    public int Id { get; private set; }

    /// <summary>Owning user ID.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Creation timestamp.</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    /// <summary>Wishlist items.</summary>
    public ICollection<WishlistItem> Items { get; private set; } = new List<WishlistItem>();

    private Wishlist() { }

    /// <summary>Creates a wishlist for a user.</summary>
    public static Wishlist Create(string userId)
    {
        return new Wishlist { UserId = userId };
    }

    /// <summary>Adds a product to the wishlist.</summary>
    public WishlistItem AddItem(int productId)
    {
        if (Items.Any(i => i.ProductId == productId))
            throw new Exceptions.BusinessRuleException("المنتج موجود في المفضلة بالفعل", "Product already in wishlist.");

        var item = WishlistItem.Create(Id, productId);
        Items.Add(item);
        return item;
    }

    /// <summary>Removes a product from the wishlist.</summary>
    public void RemoveItem(int productId)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new Exceptions.NotFoundException("المنتج غير موجود في المفضلة", "Product not in wishlist.");
        Items.Remove(item);
    }

    /// <summary>Checks if a product is in the wishlist.</summary>
    public bool ContainsProduct(int productId) => Items.Any(i => i.ProductId == productId);
}

/// <summary>Individual item in a wishlist.</summary>
public sealed class WishlistItem
{
    /// <summary>Primary key.</summary>
    public int Id { get; private set; }

    /// <summary>Wishlist ID.</summary>
    public int WishlistId { get; private set; }

    /// <summary>Product ID.</summary>
    public int ProductId { get; private set; }

    /// <summary>When the item was added.</summary>
    public DateTime AddedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    /// <summary>The wishlist.</summary>
    public Wishlist Wishlist { get; private set; } = null!;

    /// <summary>The product.</summary>
    public Product Product { get; private set; } = null!;

    private WishlistItem() { }

    /// <summary>Creates a wishlist item.</summary>
    public static WishlistItem Create(int wishlistId, int productId)
    {
        return new WishlistItem
        {
            WishlistId = wishlistId,
            ProductId = productId
        };
    }
}
