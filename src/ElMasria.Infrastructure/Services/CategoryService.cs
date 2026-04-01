using AutoMapper;
using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Category;
using ElMasria.Application.Interfaces;
using ElMasria.Domain.Entities;
using ElMasria.Domain.Exceptions;
using ElMasria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ElMasria.Infrastructure.Services;

/// <summary>
/// Implementation of category service handling nested subcategories and path logic.
/// </summary>
public sealed class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;

    /// <summary>Initializes CategoryService.</summary>
    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<IReadOnlyList<CategoryDto>>> GetHierarchyAsync(CancellationToken ct = default)
    {
        // For performance, getting all active categories and building tree in memory
        var allCategories = await _unitOfWork.Categories.GetAllAsync(ct);
        
        var roots = allCategories
            .Where(c => c.ParentId == null && c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToList();

        var dtos = _mapper.Map<IReadOnlyList<CategoryDto>>(roots);
        return ApiResponse<IReadOnlyList<CategoryDto>>.Ok(dtos, "تم جلب شجرة التصنيفات", "Category hierarchy retrieved.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<CategoryDetailDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var category = await _unitOfWork.Categories.GetByIdWithSubcategoriesAsync(id, ct);
        if (category is null || !category.IsActive)
            return ApiResponse<CategoryDetailDto>.Fail(404, "التصنيف غير موجود", "Category not found.");

        var dto = _mapper.Map<CategoryDetailDto>(category);
        dto = dto with { Breadcrumbs = await GetBreadcrumbsAsync(category.Id, ct) };

        return ApiResponse<CategoryDetailDto>.Ok(dto);
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<CategoryDetailDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        // First get the category ID by slug without eagerly loading everything
        var categories = await _unitOfWork.Categories.FindAsync(c => c.Slug == slug.ToLowerInvariant() && !c.IsDeleted && c.IsActive);
        var categoryHeader = categories.FirstOrDefault();
        
        if (categoryHeader is null)
            return ApiResponse<CategoryDetailDto>.Fail(404, "التصنيف غير موجود", "Category not found.");

        // Now fetch with subcategories properly
        var category = await _unitOfWork.Categories.GetByIdWithSubcategoriesAsync(categoryHeader.Id, ct);
        var dto = _mapper.Map<CategoryDetailDto>(category);
        dto = dto with { Breadcrumbs = await GetBreadcrumbsAsync(category!.Id, ct) };

        return ApiResponse<CategoryDetailDto>.Ok(dto);
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<CategoryDetailDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (request.ParentId.HasValue)
        {
            var parent = await _unitOfWork.Categories.GetByIdAsync(request.ParentId.Value, ct);
            if (parent is null)
                return ApiResponse<CategoryDetailDto>.Fail(400, "التصنيف الأب غير موجود", "Parent category not found.");
        }

        var slug = GenerateSlug(request.NameEn);
        var existingSlug = await _unitOfWork.Categories.FindAsync(c => c.Slug == slug && !c.IsDeleted);
        if (existingSlug.Any())
            slug = $"{slug}-{Guid.NewGuid().ToString("N")[..4]}";

        var category = Category.Create(
            request.NameAr, request.NameEn, slug,
            request.DescriptionAr, request.DescriptionEn,
            request.ParentId, request.ImageUrl, request.DisplayOrder);

        await _unitOfWork.Categories.AddAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var created = await _unitOfWork.Categories.GetByIdWithSubcategoriesAsync(category.Id, ct);
        var dto = _mapper.Map<CategoryDetailDto>(created);
        dto = dto with { Breadcrumbs = await GetBreadcrumbsAsync(created!.Id, ct) };

        _logger.LogInformation("Category created: {CategoryId} - {Slug}", category.Id, category.Slug);
        return ApiResponse<CategoryDetailDto>.Created(dto, "تم إنشاء التصنيف بنجاح", "Category created successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<CategoryDetailDto>> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await _unitOfWork.Categories.GetByIdWithSubcategoriesAsync(id, ct);
        if (category is null)
            return ApiResponse<CategoryDetailDto>.Fail(404, "التصنيف غير موجود", "Category not found.");

        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == category.Id)
                return ApiResponse<CategoryDetailDto>.Fail(400, "التصنيف لا يمكن أن يكون أباً لنفسه", "Category cannot be its own parent.");

            var parent = await _unitOfWork.Categories.GetByIdAsync(request.ParentId.Value, ct);
            if (parent is null)
                return ApiResponse<CategoryDetailDto>.Fail(400, "التصنيف الأب غير موجود", "Parent category not found.");

            // Prevent circular references (simple 1-level check, could be recursive)
            if (parent.ParentId == category.Id)
                return ApiResponse<CategoryDetailDto>.Fail(400, "مرجع دائري غير مسموح", "Circular reference detected.");
        }

        var slug = GenerateSlug(request.NameEn);
        if (category.Slug != slug)
        {
            var existingSlug = await _unitOfWork.Categories.FindAsync(c => c.Slug == slug && c.Id != id && !c.IsDeleted);
            if (existingSlug.Any())
                slug = $"{slug}-{Guid.NewGuid().ToString("N")[..4]}";
        }

        category.Update(
            request.NameAr, request.NameEn, slug,
            request.DescriptionAr, request.DescriptionEn,
            request.ImageUrl, request.DisplayOrder);
        category.SetParent(request.ParentId);

        if (request.IsActive)
            category.Activate();
        else
            category.Deactivate();

        await _unitOfWork.SaveChangesAsync(ct);

        var dto = _mapper.Map<CategoryDetailDto>(category);
        dto = dto with { Breadcrumbs = await GetBreadcrumbsAsync(category.Id, ct) };

        _logger.LogInformation("Category updated: {CategoryId}", id);
        return ApiResponse<CategoryDetailDto>.Ok(dto, "تم تحديث التصنيف بنجاح", "Category updated successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await _unitOfWork.Categories.GetByIdWithSubcategoriesAsync(id, ct);
        if (category is null)
            return ApiResponse<bool>.Fail(404, "التصنيف غير موجود", "Category not found.");

        if (category.Children.Any(sc => !sc.IsDeleted))
            return ApiResponse<bool>.Fail(400, "لا يمكن حذف تصنيف يحتوي على تصنيفات فرعية", "Cannot delete category containing active subcategories.");

        var relatedProducts = await _unitOfWork.Products.FindAsync(p => p.CategoryId == id && !p.IsDeleted, ct);
        if (relatedProducts.Count > 0)
            return ApiResponse<bool>.Fail(400, "لا يمكن حذف تصنيف مرتبط بمنتجات", $"Cannot delete category linked to {relatedProducts.Count} products.");

        category.SoftDelete();
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Category soft-deleted: {CategoryId}", id);
        return ApiResponse<bool>.Ok(true, "تم حذف التصنيف بنجاح", "Category deleted successfully.");
    }

    // ── Helpers ──────────────────────────────────────────────────

    private async Task<IReadOnlyList<CategoryBreadcrumbDto>> GetBreadcrumbsAsync(int categoryId, CancellationToken ct)
    {
        var breadcrumbs = new List<CategoryBreadcrumbDto>();
        var allCats = await _unitOfWork.Categories.GetAllAsync(ct); // Usually cached in production
        var lookup = allCats.ToDictionary(c => c.Id);

        var currentId = (int?)categoryId;
        while (currentId.HasValue && lookup.TryGetValue(currentId.Value, out var currentCat))
        {
            breadcrumbs.Insert(0, _mapper.Map<CategoryBreadcrumbDto>(currentCat));
            currentId = currentCat.ParentId;
        }

        return breadcrumbs.AsReadOnly();
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("&", "and")
            .Replace("/", "-");
    }
}
