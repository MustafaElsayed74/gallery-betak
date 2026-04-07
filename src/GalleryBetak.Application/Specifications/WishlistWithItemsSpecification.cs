using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Specifications;

namespace GalleryBetak.Application.Specifications;

/// <summary>
/// Specification to retrieve a wishlist with its items and associated products eagerly loaded.
/// </summary>
public sealed class WishlistWithItemsSpecification : BaseSpecification<Wishlist>
{
    /// <summary>Initializes specification by User ID.</summary>
    public WishlistWithItemsSpecification(string userId) 
        : base(x => x.UserId == userId)
    {
        AddInclude("Items");
        AddInclude("Items.Product");
        AddInclude("Items.Product.Images");
    }
}

