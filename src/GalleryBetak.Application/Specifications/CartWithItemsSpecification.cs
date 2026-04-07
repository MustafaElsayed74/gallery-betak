using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Specifications;

namespace GalleryBetak.Application.Specifications;

/// <summary>
/// Specification to retrieve a cart with its items and associated products eagerly loaded.
/// </summary>
public sealed class CartWithItemsSpecification : BaseSpecification<Cart>
{
    /// <summary>Initializes specification by User ID.</summary>
    public CartWithItemsSpecification(string userId, bool isUser = true) 
        : base(x => x.UserId == userId)
    {
        AddInclude("Items");
        AddInclude("Items.Product");
        AddInclude("Items.Product.Images");
    }

    /// <summary>Initializes specification by Session ID.</summary>
    public CartWithItemsSpecification(string sessionId, int dummyValue = 0) 
        : base(x => x.SessionId == sessionId)
    {
        AddInclude("Items");
        AddInclude("Items.Product");
        AddInclude("Items.Product.Images");
    }
}

