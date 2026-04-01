using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Category;
using ElMasria.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElMasria.API.Controllers;

/// <summary>
/// Category endpoints: browsing catalog tree and admin operations.
/// </summary>
public class CategoriesController : BaseApiController
{
    private readonly ICategoryService _categoryService;

    /// <summary>Initializes CategoriesController.</summary>
    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Gets the complete hierarchical tree of active categories.
    /// Used for navigation menus.
    /// </summary>
    /// <response code="200">Category tree retrieved.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHierarchy()
    {
        var result = await _categoryService.GetHierarchyAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets detailed category info by URL slug, including breadcrumbs.
    /// </summary>
    /// <response code="200">Category details.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("by-slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var result = await _categoryService.GetBySlugAsync(slug);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets a category by its ID.
    /// </summary>
    /// <response code="200">Category details.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _categoryService.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Creates a new category (admin only).
    /// </summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation error.</response>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var result = await _categoryService.CreateAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Updates an existing category (admin only).
    /// </summary>
    /// <response code="200">Category updated.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        var result = await _categoryService.UpdateAsync(id, request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Soft-deletes a category. Unlinks or prevents delete if products/subcategories exist (admin only).
    /// </summary>
    /// <response code="200">Category deleted.</response>
    /// <response code="400">Category in use.</response>
    /// <response code="404">Category not found.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _categoryService.DeleteAsync(id);
        return StatusCode(result.StatusCode, result);
    }
}
