using System.ComponentModel.DataAnnotations;

namespace GalleryBetak.Application.DTOs.Cart;

/// <summary>Response containing full cart details.</summary>
public sealed record CartDto
{
    /// <summary>Cart identifier.</summary>
    public int Id { get; init; }
    
    /// <summary>Subtotal of all items.</summary>
    public decimal SubTotal { get; init; }
    
    /// <summary>Total number of items (sum of quantities).</summary>
    public int TotalItems { get; init; }
    
    /// <summary>The cart items.</summary>
    public IReadOnlyList<CartItemDto> Items { get; init; } = [];
}

/// <summary>Individual cart item detail.</summary>
public sealed record CartItemDto
{
    public int ProductId { get; init; }
    public string ProductNameAr { get; init; } = string.Empty;
    public string ProductNameEn { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal TotalPrice { get; init; }
    public int StockQuantity { get; init; } // To validate if they can increase quantity
}

/// <summary>Request to add an item to the cart.</summary>
public sealed record AddToCartRequest
{
    [Required]
    public int ProductId { get; init; }
    
    [Range(1, 99, ErrorMessage = "الكمية يجب أن تكون بين 1 و 99")]
    public int Quantity { get; init; } = 1;
}

/// <summary>Request to update the quantity of a cart item.</summary>
public sealed record UpdateCartItemRequest
{
    [Range(1, 99, ErrorMessage = "الكمية يجب أن تكون بين 1 و 99")]
    public int Quantity { get; init; }
}

