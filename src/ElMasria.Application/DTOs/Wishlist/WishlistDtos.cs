namespace ElMasria.Application.DTOs.Wishlist;

/// <summary>Response containing full wishlist details.</summary>
public sealed record WishlistDto
{
    /// <summary>Wishlist identifier.</summary>
    public int Id { get; init; }
    
    /// <summary>The saved items.</summary>
    public IReadOnlyList<WishlistItemDto> Items { get; init; } = [];
}

/// <summary>Individual wishlist item detail.</summary>
public sealed record WishlistItemDto
{
    public int ProductId { get; init; }
    public string ProductNameAr { get; init; } = string.Empty;
    public string ProductNameEn { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal UnitPrice { get; init; }
    public int StockQuantity { get; init; } 
    public bool IsInStock => StockQuantity > 0;
    public DateTime AddedAt { get; init; }
}
