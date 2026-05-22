using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GalleryBetak.API.Controllers;

public class ReviewsController : BaseApiController
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Creates a new product review.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ReviewDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
    {
        var result = await _reviewService.CreateReviewAsync(GetUserId(), request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Retrieves approved reviews for a product.</summary>
    [HttpGet("product/{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductReviews(int productId)
    {
        var result = await _reviewService.GetProductReviewsAsync(productId);
        return StatusCode(result.StatusCode, result);
    }
}
