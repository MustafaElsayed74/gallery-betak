using AutoMapper;
using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Product;
using ElMasria.Application.Interfaces;
using ElMasria.Domain.Entities;
using ElMasria.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ElMasria.Infrastructure.Services;

/// <summary>
/// Product service implementation with full catalog, search, and admin CRUD.
/// </summary>
public sealed class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    /// <summary>Initializes ProductService.</summary>
    public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<PagedResult<ProductListDto>>> GetProductsAsync(
        ProductQueryParams query, CancellationToken ct = default)
    {
        var skip = (query.PageNumber - 1) * query.PageSize;
        
        var spec = new ElMasria.Application.Specifications.ProductWithCategorySpecification(
            query.Search, query.CategoryId, null, skip, query.PageSize);
            
        var countSpec = new ElMasria.Application.Specifications.ProductFiltersForCountSpecification(
            query.Search, query.CategoryId);

        var items = await _unitOfWork.Products.ListAsync(spec, ct);
        var total = await _unitOfWork.Products.CountAsync(countSpec, ct);

        var dtos = _mapper.Map<IReadOnlyList<ProductListDto>>(items);
        var result = new PagedResult<ProductListDto>
        {
            Items = dtos,
            TotalCount = total,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        return ApiResponse<PagedResult<ProductListDto>>.Ok(result, result.ToMeta(),
            "تم جلب المنتجات", "Products retrieved.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<ProductDetailDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var spec = new ElMasria.Application.Specifications.ProductWithCategorySpecification(id);
        var product = await _unitOfWork.Products.GetEntityWithSpecAsync(spec, ct);
        if (product is null)
            return ApiResponse<ProductDetailDto>.Fail(404, "المنتج غير موجود", "Product not found.");

        product.IncrementViewCount();
        await _unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<ProductDetailDto>.Ok(_mapper.Map<ProductDetailDto>(product));
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<ProductDetailDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var spec = new ElMasria.Application.Specifications.ProductWithCategorySpecification(slug);
        var product = await _unitOfWork.Products.GetEntityWithSpecAsync(spec, ct);
        if (product is null)
            return ApiResponse<ProductDetailDto>.Fail(404, "المنتج غير موجود", "Product not found.");

        product.IncrementViewCount();
        await _unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<ProductDetailDto>.Ok(_mapper.Map<ProductDetailDto>(product));
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<IReadOnlyList<ProductListDto>>> GetFeaturedAsync(
        int count = 8, CancellationToken ct = default)
    {
        var products = await _unitOfWork.Products.GetFeaturedAsync(count, ct);
        var dtos = _mapper.Map<IReadOnlyList<ProductListDto>>(products);
        return ApiResponse<IReadOnlyList<ProductListDto>>.Ok(dtos,
            "تم جلب المنتجات المميزة", "Featured products retrieved.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<ProductDetailDto>> CreateAsync(
        CreateProductRequest request, CancellationToken ct = default)
    {
        // Check SKU uniqueness
        var existingSku = await _unitOfWork.Products.GetBySKUAsync(request.SKU, ct);
        if (existingSku is not null)
            return ApiResponse<ProductDetailDto>.Fail(409,
                "كود المنتج (SKU) مستخدم بالفعل", "SKU already exists.");

        // Validate category exists
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, ct);
        if (category is null)
            return ApiResponse<ProductDetailDto>.Fail(400,
                "التصنيف غير موجود", "Category not found.");

        // Create product using domain factory
        var product = Product.Create(
            request.NameAr, request.NameEn,
            request.DescriptionAr, request.DescriptionEn,
            request.Price, request.SKU, request.StockQuantity, request.CategoryId);

        if (request.OriginalPrice.HasValue)
            product.UpdatePrice(request.Price, request.OriginalPrice.Value);

        if (request.IsFeatured)
            product.SetFeatured(true);

        // Add tags
        foreach (var tagId in request.TagIds)
        {
            var tag = await _unitOfWork.Tags.GetByIdAsync(tagId, ct);
            if (tag is not null)
                product.AddTag(tagId);
        }

        await _unitOfWork.Products.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Reload with includes using specification
        var spec = new ElMasria.Application.Specifications.ProductWithCategorySpecification(product.Id);
        var created = await _unitOfWork.Products.GetEntityWithSpecAsync(spec, ct);
        _logger.LogInformation("Product created: {ProductId} - {SKU}", product.Id, product.SKU);

        return ApiResponse<ProductDetailDto>.Created(
            _mapper.Map<ProductDetailDto>(created!),
            "تم إنشاء المنتج بنجاح", "Product created successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<ProductDetailDto>> UpdateAsync(
        int id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var spec = new ElMasria.Application.Specifications.ProductWithCategorySpecification(id);
        var product = await _unitOfWork.Products.GetEntityWithSpecAsync(spec, ct);
        if (product is null)
            return ApiResponse<ProductDetailDto>.Fail(404, "المنتج غير موجود", "Product not found.");

        // Validate category
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, ct);
        if (category is null)
            return ApiResponse<ProductDetailDto>.Fail(400, "التصنيف غير موجود", "Category not found.");

        // Update domain entity
        product.Update(request.NameAr, request.NameEn,
            request.DescriptionAr, request.DescriptionEn,
            request.CategoryId);

        product.UpdatePrice(request.Price, request.OriginalPrice);
        product.UpdateStock(request.StockQuantity);
        product.SetFeatured(request.IsFeatured);
        product.SetActive(request.IsActive);

        // Replace tags
        product.ClearTags();
        foreach (var tagId in request.TagIds)
        {
            var tag = await _unitOfWork.Tags.GetByIdAsync(tagId, ct);
            if (tag is not null)
                product.AddTag(tagId);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        var updatedSpec = new ElMasria.Application.Specifications.ProductWithCategorySpecification(id);
        var updated = await _unitOfWork.Products.GetEntityWithSpecAsync(updatedSpec, ct);
        _logger.LogInformation("Product updated: {ProductId}", id);

        return ApiResponse<ProductDetailDto>.Ok(
            _mapper.Map<ProductDetailDto>(updated!),
            "تم تحديث المنتج بنجاح", "Product updated successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, ct);
        if (product is null)
            return ApiResponse<bool>.Fail(404, "المنتج غير موجود", "Product not found.");

        product.SoftDelete();
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product soft-deleted: {ProductId}", id);
        return ApiResponse<bool>.Ok(true, "تم حذف المنتج بنجاح", "Product deleted successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<IReadOnlyList<ProductListDto>>> GetLowStockAsync(
        int threshold = 5, CancellationToken ct = default)
    {
        var products = await _unitOfWork.Products.GetLowStockAsync(threshold, ct);
        var dtos = _mapper.Map<IReadOnlyList<ProductListDto>>(products);
        return ApiResponse<IReadOnlyList<ProductListDto>>.Ok(dtos,
            "تم جلب المنتجات منخفضة المخزون", "Low stock products retrieved.");
    }
}
