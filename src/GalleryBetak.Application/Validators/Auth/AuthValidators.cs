using System.Text.RegularExpressions;
using GalleryBetak.Application.DTOs.Auth;
using FluentValidation;

namespace GalleryBetak.Application.Validators.Auth;

/// <summary>Validates login request.</summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة");
    }
}

/// <summary>Validates registration request.</summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private static readonly Regex EgyptPhoneRegex = new(@"^01[0125]\d{8}$", RegexOptions.Compiled);

    /// <summary>Initializes validation rules.</summary>
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("الاسم الأول مطلوب")
            .MaximumLength(100).WithMessage("الاسم الأول لا يتجاوز 100 حرف");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("الاسم الأخير مطلوب")
            .MaximumLength(100).WithMessage("الاسم الأخير لا يتجاوز 100 حرف");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة")
            .MinimumLength(8).WithMessage("كلمة المرور يجب أن تكون 8 أحرف على الأقل")
            .Matches(@"[A-Z]").WithMessage("كلمة المرور يجب أن تحتوي على حرف كبير")
            .Matches(@"[a-z]").WithMessage("كلمة المرور يجب أن تحتوي على حرف صغير")
            .Matches(@"\d").WithMessage("كلمة المرور يجب أن تحتوي على رقم")
            .Matches(@"[^\da-zA-Z]").WithMessage("كلمة المرور يجب أن تحتوي على رمز خاص");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("تأكيد كلمة المرور مطلوب")
            .Equal(x => x.Password).WithMessage("كلمة المرور وتأكيدها غير متطابقتين");

        RuleFor(x => x.PhoneNumber)
            .Must(phone => phone is null || EgyptPhoneRegex.IsMatch(phone))
            .WithMessage("رقم الهاتف غير صحيح. يجب أن يبدأ بـ 01 ويتكون من 11 رقم");
    }
}

/// <summary>Validates refresh token request.</summary>
public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("رمز الوصول مطلوب");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("رمز التحديث مطلوب");
    }
}

/// <summary>Validates change password request.</summary>
public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    /// <summary>Initializes validation rules.</summary>
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("كلمة المرور الحالية مطلوبة");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("كلمة المرور الجديدة مطلوبة")
            .MinimumLength(8).WithMessage("كلمة المرور يجب أن تكون 8 أحرف على الأقل")
            .Matches(@"[A-Z]").WithMessage("كلمة المرور يجب أن تحتوي على حرف كبير")
            .Matches(@"[a-z]").WithMessage("كلمة المرور يجب أن تحتوي على حرف صغير")
            .Matches(@"\d").WithMessage("كلمة المرور يجب أن تحتوي على رقم")
            .Matches(@"[^\da-zA-Z]").WithMessage("كلمة المرور يجب أن تحتوي على رمز خاص");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("تأكيد كلمة المرور الجديدة مطلوب")
            .Equal(x => x.NewPassword).WithMessage("كلمة المرور الجديدة وتأكيدها غير متطابقتين");
    }
}

/// <summary>Validates profile update request.</summary>
public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    private static readonly Regex EgyptPhoneRegex = new(@"^01[0125]\d{8}$", RegexOptions.Compiled);

    /// <summary>Initializes validation rules.</summary>
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("الاسم الأول مطلوب")
            .MaximumLength(100).WithMessage("الاسم الأول لا يتجاوز 100 حرف");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("الاسم الأخير مطلوب")
            .MaximumLength(100).WithMessage("الاسم الأخير لا يتجاوز 100 حرف");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");

        RuleFor(x => x.PhoneNumber)
            .Must(phone => phone is null || EgyptPhoneRegex.IsMatch(phone))
            .WithMessage("رقم الهاتف غير صحيح. يجب أن يبدأ بـ 01 ويتكون من 11 رقم");
    }
}

/// <summary>Validates create/update address request.</summary>
public sealed class UpsertAddressRequestValidator : AbstractValidator<UpsertAddressRequest>
{
    private static readonly Regex EgyptPhoneRegex = new(@"^01[0125]\d{8}$", RegexOptions.Compiled);

    /// <summary>Initializes validation rules.</summary>
    public UpsertAddressRequestValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("اسم العنوان مطلوب")
            .MaximumLength(100).WithMessage("اسم العنوان لا يتجاوز 100 حرف");

        RuleFor(x => x.RecipientName)
            .NotEmpty().WithMessage("اسم المستلم مطلوب")
            .MaximumLength(200).WithMessage("اسم المستلم لا يتجاوز 200 حرف");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("رقم الهاتف مطلوب")
            .Must(phone => EgyptPhoneRegex.IsMatch(phone))
            .WithMessage("رقم الهاتف غير صحيح. يجب أن يبدأ بـ 01 ويتكون من 11 رقم");

        RuleFor(x => x.Governorate)
            .NotEmpty().WithMessage("المحافظة مطلوبة")
            .MaximumLength(100).WithMessage("المحافظة لا تتجاوز 100 حرف");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("المدينة مطلوبة")
            .MaximumLength(100).WithMessage("المدينة لا تتجاوز 100 حرف");

        RuleFor(x => x.StreetAddress)
            .NotEmpty().WithMessage("العنوان التفصيلي مطلوب")
            .MaximumLength(300).WithMessage("العنوان التفصيلي لا يتجاوز 300 حرف");

        RuleFor(x => x.District)
            .MaximumLength(120).WithMessage("الحي لا يتجاوز 120 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.District));

        RuleFor(x => x.BuildingNo)
            .MaximumLength(50).WithMessage("رقم المبنى لا يتجاوز 50 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.BuildingNo));

        RuleFor(x => x.ApartmentNo)
            .MaximumLength(50).WithMessage("رقم الشقة لا يتجاوز 50 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.ApartmentNo));

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("الرمز البريدي لا يتجاوز 20 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.PostalCode));
    }
}

