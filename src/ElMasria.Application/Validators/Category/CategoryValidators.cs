using ElMasria.Application.DTOs.Category;
using FluentValidation;

namespace ElMasria.Application.Validators.Category;

/// <summary>Validates create category request.</summary>
public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم التصنيف بالعربية مطلوب")
            .MaximumLength(150).WithMessage("اسم التصنيف لا يتجاوز 150 حرف");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("اسم التصنيف بالإنجليزية مطلوب")
            .MaximumLength(150).WithMessage("Category name max 150 characters");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("ترتيب العرض يجب أن يكون 0 أو أكثر");

        RuleFor(x => x.ParentId)
            .GreaterThan(0).When(x => x.ParentId.HasValue).WithMessage("معرف التصنيف الأب غير صالح");
    }
}

/// <summary>Validates update category request.</summary>
public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم التصنيف بالعربية مطلوب")
            .MaximumLength(150).WithMessage("اسم التصنيف لا يتجاوز 150 حرف");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("اسم التصنيف بالإنجليزية مطلوب")
            .MaximumLength(150).WithMessage("Category name max 150 characters");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("ترتيب العرض يجب أن يكون 0 أو أكثر");

        RuleFor(x => x.ParentId)
            .GreaterThan(0).When(x => x.ParentId.HasValue).WithMessage("معرف التصنيف الأب غير صالح");
    }
}
