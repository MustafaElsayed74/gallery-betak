using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Product;
using ElMasria.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElMasria.API.Controllers;

/// <summary>
/// Product endpoints: catalog browsing, search, and admin CRUD.
/// </summary>
public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;

    /// <summary>Initializes ProductsController.</summary>
    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Gets a paginated, filtered list of products.
    /// Supports search, category filtering, price range, and sorting.
    /// </summary>
    /// <response code="200">Products retrieved with pagination metadata.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] ProductQueryParams query)
    {
        var result = await _productService.GetProductsAsync(query);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets featured products for homepage display.
    /// </summary>
    /// <param name="count">Number of featured products (default 8, max 20).</param>
    /// <response code="200">Featured products retrieved.</response>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatured([FromQuery] int count = 8)
    {
        if (count > 20) count = 20;
        var result = await _productService.GetFeaturedAsync(count);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets a product by its URL-friendly slug.
    /// </summary>
    /// <response code="200">Product details.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("by-slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var result = await _productService.GetBySlugAsync(slug);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets a product by its ID with full details.
    /// </summary>
    /// <response code="200">Product details.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _productService.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Creates a new product (admin only).
    /// </summary>
    /// <response code="201">Product created.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="409">SKU conflict.</response>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var result = await _productService.CreateAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Updates a product (admin only).
    /// </summary>
    /// <response code="200">Product updated.</response>
    /// <response code="404">Product not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
    {
        var result = await _productService.UpdateAsync(id, request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Soft-deletes a product (admin only).
    /// </summary>
    /// <response code="200">Product deleted.</response>
    /// <response code="404">Product not found.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _productService.DeleteAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets low-stock products (admin alert).
    /// </summary>
    /// <param name="threshold">Stock threshold (default 5).</param>
    /// <response code="200">Low stock product list.</response>
    [HttpGet("low-stock")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock([FromQuery] int threshold = 5)
    {
        var result = await _productService.GetLowStockAsync(threshold);
        return StatusCode(result.StatusCode, result);
    }
}
