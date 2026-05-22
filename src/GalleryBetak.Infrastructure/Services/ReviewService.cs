using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Enums;
using GalleryBetak.Domain.Interfaces;

namespace GalleryBetak.Infrastructure.Services;

public sealed class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ReviewDto>> CreateReviewAsync(string userId, CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Verify product exists
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
            return ApiResponse<ReviewDto>.Fail(StatusCodes.Status404NotFound, "المنتج غير موجود", "Product not found.");

        // 2. Verify user hasn't already reviewed
        var existingReview = await _unitOfWork.Reviews.FirstOrDefaultAsync(r => r.ProductId == request.ProductId && r.UserId == userId, cancellationToken);
        if (existingReview != null)
            return ApiResponse<ReviewDto>.Fail(StatusCodes.Status400BadRequest, "لقد قمت بتقييم هذا المنتج بالفعل", "You have already reviewed this product.");

        // 3. Verify purchase (only delivered orders)
        bool hasPurchased = await _unitOfWork.Orders.HasUserPurchasedProductAsync(userId, request.ProductId, cancellationToken);
        if (!hasPurchased)
            return ApiResponse<ReviewDto>.Fail(StatusCodes.Status403Forbidden, "يمكنك فقط تقييم المنتجات التي قمت بشرائها واستلامها", "You can only review products you have purchased and received.");

        // 4. Create review
        var review = Review.Create(
            productId: request.ProductId,
            userId: userId,
            rating: request.Rating,
            comment: request.Comment,
            isVerifiedPurchase: true // Always true because of the check above
        );

        // Auto-approve for this demo
        review.Approve();

        await _unitOfWork.Reviews.AddAsync(review, cancellationToken);
        
        // Recalculate rating
        var approvedReviews = await _unitOfWork.Reviews.FindAsync(
            r => r.ProductId == request.ProductId && r.Status == ReviewStatus.Approved,
            cancellationToken);
            
        // Include the newly added review which is auto-approved
        var allReviews = approvedReviews.ToList();
        allReviews.Add(review);

        var avgRating = allReviews.Any() ? (decimal)allReviews.Average(r => r.Rating) : 0m;
        product.RecalculateRating(avgRating, allReviews.Count);
        
        _unitOfWork.Products.Update(product);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ReviewDto>.Created(new ReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserId = review.UserId,
            UserName = "User", // Would normally come from user manager
            Rating = review.Rating,
            Comment = review.Comment,
            Status = review.Status.ToString(),
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            CreatedAt = review.CreatedAt
        });
    }

    public async Task<ApiResponse<IReadOnlyList<ReviewDto>>> GetProductReviewsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var reviews = await _unitOfWork.Reviews.FindAsync(
            r => r.ProductId == productId && r.Status == ReviewStatus.Approved,
            cancellationToken);

        var dtos = reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            ProductId = r.ProductId,
            UserId = r.UserId,
            UserName = "Verified Buyer", // Anonymous for privacy unless we fetch user details
            Rating = r.Rating,
            Comment = r.Comment,
            Status = r.Status.ToString(),
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            CreatedAt = r.CreatedAt
        }).ToList();

        return ApiResponse<IReadOnlyList<ReviewDto>>.Ok(dtos);
    }
}
