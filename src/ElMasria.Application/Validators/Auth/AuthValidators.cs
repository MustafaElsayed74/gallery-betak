using System.Text.RegularExpressions;
using ElMasria.Application.DTOs.Auth;
using FluentValidation;

namespace ElMasria.Application.Validators.Auth;

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
