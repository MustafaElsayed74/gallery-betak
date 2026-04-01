using ElMasria.Application.DTOs.Product;
using FluentValidation;

namespace ElMasria.Application.Validators.Product;

/// <summary>Validates create product request.</summary>
public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم المنتج بالعربية مطلوب")
            .MaximumLength(300).WithMessage("اسم المنتج لا يتجاوز 300 حرف");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("اسم المنتج بالإنجليزية مطلوب")
            .MaximumLength(300).WithMessage("Product name max 300 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("السعر يجب أن يكون أكبر من صفر");

        RuleFor(x => x.OriginalPrice)
            .GreaterThan(x => x.Price)
            .When(x => x.OriginalPrice.HasValue)
            .WithMessage("السعر الأصلي يجب أن يكون أكبر من السعر الحالي");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("كود المنتج (SKU) مطلوب")
            .MaximumLength(50).WithMessage("كود المنتج لا يتجاوز 50 حرف");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("الكمية لا يمكن أن تكون سالبة");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("التصنيف مطلوب");

        RuleFor(x => x.Weight)
            .GreaterThan(0).When(x => x.Weight.HasValue)
            .WithMessage("الوزن يجب أن يكون أكبر من صفر");
    }
}

/// <summary>Validates update product request.</summary>
public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم المنتج بالعربية مطلوب")
            .MaximumLength(300).WithMessage("اسم المنتج لا يتجاوز 300 حرف");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("اسم المنتج بالإنجليزية مطلوب")
            .MaximumLength(300).WithMessage("Product name max 300 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("السعر يجب أن يكون أكبر من صفر");

        RuleFor(x => x.OriginalPrice)
            .GreaterThan(x => x.Price)
            .When(x => x.OriginalPrice.HasValue)
            .WithMessage("السعر الأصلي يجب أن يكون أكبر من السعر الحالي");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("الكمية لا يمكن أن تكون سالبة");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("التصنيف مطلوب");
    }
}

/// <summary>Validates product query parameters.</summary>
public sealed class ProductQueryParamsValidator : AbstractValidator<ProductQueryParams>
{
    /// <summary>Initializes validation rules.</summary>
    public ProductQueryParamsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("رقم الصفحة يجب أن يكون 1 على الأقل");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("حجم الصفحة يجب أن يكون بين 1 و 100");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue)
            .WithMessage("الحد الأدنى للسعر لا يمكن أن يكون سالباً");

        RuleFor(x => x.MaxPrice)
            .GreaterThan(x => x.MinPrice ?? 0)
            .When(x => x.MaxPrice.HasValue)
            .WithMessage("الحد الأقصى للسعر يجب أن يكون أكبر من الحد الأدنى");

        RuleFor(x => x.SortBy)
            .Must(s => s is null or "price" or "name" or "rating" or "newest" or "views")
            .WithMessage("الترتيب يجب أن يكون: price, name, rating, newest, views");

        RuleFor(x => x.SortDirection)
            .Must(s => s is null or "asc" or "desc")
            .WithMessage("اتجاه الترتيب يجب أن يكون: asc أو desc");
    }
}
