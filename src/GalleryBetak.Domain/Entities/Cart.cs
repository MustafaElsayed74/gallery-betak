namespace GalleryBetak.Domain.Entities;

/// <summary>
/// Shopping cart. Supports both authenticated users (UserId) and guests (SessionId).
/// Each user/session has at most one active cart.
/// </summary>
public sealed class Cart : BaseEntity
{
    /// <summary>User ID (null for guest carts).</summary>
    public string? UserId { get; private set; }

    /// <summary>Session ID for guest carts.</summary>
    public string? SessionId { get; private set; }

    /// <summary>Applied coupon ID.</summary>
    public int? CouponId { get; private set; }

    /// <summary>Cart expiry time (for cleanup).</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Last update timestamp.</summary>
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    /// <summary>Items in the cart.</summary>
    public ICollection<CartItem> Items { get; private set; } = new List<CartItem>();

    /// <summary>Applied coupon.</summary>
    public Coupon? Coupon { get; private set; }

    private Cart() { }

    /// <summary>Creates a cart for an authenticated user.</summary>
    public static Cart CreateForUser(string userId, TimeSpan? expiresIn = null)
    {
        return new Cart
        {
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromDays(30))
        };
    }

    /// <summary>Creates a cart for a guest session.</summary>
    public static Cart CreateForGuest(string sessionId, TimeSpan? expiresIn = null)
    {
        return new Cart
        {
            SessionId = sessionId,
            ExpiresAt = DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromDays(7))
        };
    }

    /// <summary>Adds a product to the cart or increments quantity if it already exists.</summary>
    public CartItem AddItem(int productId, decimal unitPrice, int quantity = 1)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null)
        {
            existing.IncrementQuantity(quantity);
            Touch();
            return existing;
        }

        var item = CartItem.Create(Id, productId, unitPrice, quantity);
        Items.Add(item);
        Touch();
        return item;
    }

    /// <summary>Removes an item from the cart.</summary>
    public void RemoveItem(int productId)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new Exceptions.NotFoundException("المنتج غير موجود في السلة", "Product not found in cart.");
        Items.Remove(item);
        Touch();
    }

    /// <summary>Applies a coupon to the cart.</summary>
    public void ApplyCoupon(int couponId)
    {
        CouponId = couponId;
        Touch();
    }

    /// <summary>Removes the applied coupon.</summary>
    public void RemoveCoupon()
    {
        CouponId = null;
        Touch();
    }

    /// <summary>Transfers a guest cart to an authenticated user.</summary>
    public void AssignToUser(string userId)
    {
        UserId = userId;
        SessionId = null;
        ExpiresAt = DateTime.UtcNow.AddDays(30); // Extend for authenticated users
        Touch();
    }

    /// <summary>Clears all items from the cart.</summary>
    public void Clear()
    {
        Items.Clear();
        CouponId = null;
        Touch();
    }

    /// <summary>Calculates the cart subtotal.</summary>
    public decimal SubTotal => Items.Sum(i => i.TotalPrice);

    /// <summary>Total number of items.</summary>
    public int TotalItems => Items.Sum(i => i.Quantity);

    /// <summary>Whether the cart is empty.</summary>
    public bool IsEmpty => !Items.Any();

    /// <summary>Whether the cart has expired.</summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
        // Extend expiry on activity
        ExpiresAt = DateTime.UtcNow.AddDays(UserId != null ? 30 : 7);
    }
}

/// <summary>Individual item in a shopping cart.</summary>
public sealed class CartItem
{
    /// <summary>Primary key.</summary>
    public int Id { get; private set; }

    /// <summary>Cart ID.</summary>
    public int CartId { get; private set; }

    /// <summary>Product ID.</summary>
    public int ProductId { get; private set; }

    /// <summary>Quantity (1-99).</summary>
    public int Quantity { get; private set; }

    /// <summary>Unit price snapshot at add time.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>When the item was added.</summary>
    public DateTime AddedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    /// <summary>The product.</summary>
    public Product Product { get; private set; } = null!;

    /// <summary>The cart.</summary>
    public Cart Cart { get; private set; } = null!;

    /// <summary>Line total (UnitPrice × Quantity).</summary>
    public decimal TotalPrice => UnitPrice * Quantity;

    private CartItem() { }

    /// <summary>Creates a cart item.</summary>
    public static CartItem Create(int cartId, int productId, decimal unitPrice, int quantity = 1)
    {
        if (quantity <= 0 || quantity > 99)
            throw new Exceptions.DomainException("الكمية يجب أن تكون بين 1 و 99", "Quantity must be between 1 and 99.");

        return new CartItem
        {
            CartId = cartId,
            ProductId = productId,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }

    /// <summary>Updates the quantity.</summary>
    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0 || quantity > 99)
            throw new Exceptions.DomainException("الكمية يجب أن تكون بين 1 و 99", "Quantity must be between 1 and 99.");
        Quantity = quantity;
    }

    /// <summary>Increments quantity by the specified amount.</summary>
    public void IncrementQuantity(int amount = 1)
    {
        UpdateQuantity(Quantity + amount);
    }
}

