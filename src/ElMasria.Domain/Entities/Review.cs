using ElMasria.Domain.Enums;

namespace ElMasria.Domain.Entities;

/// <summary>
/// Product review with rating, moderation status, and verified purchase tracking.
/// </summary>
public sealed class Review : BaseEntity
{
    /// <summary>Product being reviewed.</summary>
    public int ProductId { get; private set; }

    /// <summary>Reviewer user ID.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Rating (1-5 stars).</summary>
    public int Rating { get; private set; }

    /// <summary>Review comment (supports Arabic).</summary>
    public string? Comment { get; private set; }

    /// <summary>Whether the reviewer actually purchased the product.</summary>
    public bool IsVerifiedPurchase { get; private set; }

    /// <summary>Moderation status (Pending, Approved, Rejected).</summary>
    public ReviewStatus Status { get; private set; } = ReviewStatus.Pending;

    // Navigation
    /// <summary>The product.</summary>
    public Product Product { get; private set; } = null!;

    /// <summary>Review images.</summary>
    public ICollection<ReviewImage> Images { get; private set; } = new List<ReviewImage>();

    private Review() { }

    /// <summary>Creates a new review.</summary>
    public static Review Create(int productId, string userId, int rating, string? comment, bool isVerifiedPurchase)
    {
        if (rating < 1 || rating > 5)
            throw new Exceptions.DomainException("التقييم يجب أن يكون بين 1 و 5", "Rating must be between 1 and 5.");

        return new Review
        {
            ProductId = productId,
            UserId = userId,
            Rating = rating,
            Comment = comment,
            IsVerifiedPurchase = isVerifiedPurchase,
            Status = ReviewStatus.Pending
        };
    }

    /// <summary>Approves the review for public display.</summary>
    public void Approve() => Status = ReviewStatus.Approved;

    /// <summary>Rejects the review.</summary>
    public void Reject() => Status = ReviewStatus.Rejected;

    /// <summary>Updates the review content.</summary>
    public void Update(int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
            throw new Exceptions.DomainException("التقييم يجب أن يكون بين 1 و 5", "Rating must be between 1 and 5.");

        Rating = rating;
        Comment = comment;
        Status = ReviewStatus.Pending; // Reset to pending after edit
    }
}

/// <summary>Image attached to a review.</summary>
public sealed class ReviewImage
{
    /// <summary>Primary key.</summary>
    public int Id { get; private set; }

    /// <summary>Review ID.</summary>
    public int ReviewId { get; private set; }

    /// <summary>Image URL.</summary>
    public string ImageUrl { get; private set; } = string.Empty;

    /// <summary>Display order.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Upload timestamp.</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    /// <summary>The review.</summary>
    public Review Review { get; private set; } = null!;

    private ReviewImage() { }

    /// <summary>Creates a review image.</summary>
    public static ReviewImage Create(int reviewId, string imageUrl, int displayOrder)
    {
        return new ReviewImage
        {
            ReviewId = reviewId,
            ImageUrl = imageUrl,
            DisplayOrder = displayOrder
        };
    }
}
