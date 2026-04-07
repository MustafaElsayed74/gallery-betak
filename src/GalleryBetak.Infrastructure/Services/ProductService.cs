using AutoMapper;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Product;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GalleryBetak.Infrastructure.Services;

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
        var sortKey = BuildSortKey(query);
        
        var spec = new GalleryBetak.Application.Specifications.ProductWithCategorySpecification(
            query.Search, query.CategoryId, sortKey, skip, query.PageSize);
            
        var countSpec = new GalleryBetak.Application.Specifications.ProductFiltersForCountSpecification(
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

    private static string? BuildSortKey(ProductQueryParams query)
    {
        if (string.IsNullOrWhiteSpace(query.SortBy))
        {
            return null;
        }

        var sortBy = query.SortBy.Trim();
        var direction = (query.SortDirection ?? "").Trim().ToLowerInvariant();

        // Support both frontend keys (priceAsc/priceDesc) and API-style keys (price + direction).
        return sortBy.ToLowerInvariant() switch
        {
            "priceasc" or "price_asc" => "priceAsc",
            "pricedesc" or "price_desc" => "priceDesc",
            "namearasc" => "nameArAsc",
            "nameenasc" => "nameEnAsc",
            "newest" => "newest",
            "rating" when direction == "asc" => "ratingAsc",
            "rating" => "ratingDesc",
            "views" when direction == "asc" => "viewsAsc",
            "views" => "viewsDesc",
            "price" when direction == "desc" => "priceDesc",
            "price" => "priceAsc",
            "name" when direction == "desc" => "nameEnAsc",
            "name" => "nameArAsc",
            _ => sortBy
        };
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<ProductDetailDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var spec = new GalleryBetak.Application.Specifications.ProductWithCategorySpecification(id);
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
        var spec = new GalleryBetak.Application.Specifications.ProductWithCategorySpecification(slug);
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

        if (!string.IsNullOrWhiteSpace(request.SourceUrl) || request.ImportedAt.HasValue)
        {
            product.SetImportMetadata(
                request.SourceUrl,
                request.ImportedAt ?? DateTime.UtcNow);
        }

        await _unitOfWork.Products.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var imageUrls = NormalizeImageUrls(request.ImageUrls);
        if (imageUrls.Count > 0)
        {
            for (var i = 0; i < imageUrls.Count; i++)
            {
                var image = ProductImage.Create(
                    product.Id,
                    imageUrls[i],
                    thumbnailUrl: null,
                    altTextAr: request.NameAr,
                    altTextEn: request.NameEn,
                    displayOrder: i,
                    isPrimary: i == 0);

                product.Images.Add(image);
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }

        // Reload with includes using specification
        var spec = new GalleryBetak.Application.Specifications.ProductWithCategorySpecification(product.Id);
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
        var spec = new GalleryBetak.Application.Specifications.ProductWithCategorySpecification(id);
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

        var updatedSpec = new GalleryBetak.Application.Specifications.ProductWithCategorySpecification(id);
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

    private static IReadOnlyList<string> NormalizeImageUrls(IEnumerable<string>? imageUrls)
    {
        if (imageUrls is null)
        {
            return [];
        }

        return imageUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Where(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
    }
}

