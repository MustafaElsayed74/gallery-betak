using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GalleryBetak.Application.Common;

namespace GalleryBetak.Application.Interfaces;

public interface IReviewService
{
    Task<ApiResponse<ReviewDto>> CreateReviewAsync(string userId, CreateReviewRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<ReviewDto>>> GetProductReviewsAsync(int productId, CancellationToken cancellationToken = default);
}

public class CreateReviewRequest
{
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class ReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
    public System.DateTime CreatedAt { get; set; }
}
