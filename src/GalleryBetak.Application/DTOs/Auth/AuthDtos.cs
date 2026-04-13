using System.ComponentModel.DataAnnotations;

namespace GalleryBetak.Application.DTOs.Auth;

/// <summary>Login request DTO.</summary>
public sealed record LoginRequest
{
    /// <summary>User email.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>Password.</summary>
    [Required]
    public string Password { get; init; } = string.Empty;
}

/// <summary>User registration request DTO.</summary>
public sealed record RegisterRequest
{
    /// <summary>First name (supports Arabic).</summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; init; } = string.Empty;

    /// <summary>Last name (supports Arabic).</summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; init; } = string.Empty;

    /// <summary>Email address.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>Password (min 8 chars, uppercase, lowercase, digit, special).</summary>
    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;

    /// <summary>Password confirmation.</summary>
    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; init; } = string.Empty;

    /// <summary>Egyptian phone number (01XXXXXXXXX).</summary>
    [Phone]
    [RegularExpression(@"^01\d{9}$", ErrorMessage = "Invalid Egyptian phone number.")]
    public string? PhoneNumber { get; init; }
}

/// <summary>Refresh token request DTO.</summary>
public sealed record RefreshTokenRequest
{
    /// <summary>The expired access token.</summary>
    [Required]
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>The refresh token.</summary>
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>Authentication response DTO.</summary>
public sealed record AuthResponse
{
    /// <summary>JWT access token.</summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>Refresh token for token rotation.</summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>Access token expiry (UTC).</summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>User profile info.</summary>
    public UserProfileDto User { get; init; } = null!;
}

/// <summary>User profile returned in auth responses.</summary>
public sealed record UserProfileDto
{
    /// <summary>User ID.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Email.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Phone number.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Full name.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>First name.</summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>Last name.</summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>Profile image URL.</summary>
    public string? ProfileImageUrl { get; init; }

    /// <summary>User roles.</summary>
    public IReadOnlyList<string> Roles { get; init; } = [];
}

/// <summary>Update current user profile request.</summary>
public sealed record UpdateProfileRequest
{
    /// <summary>First name.</summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; init; } = string.Empty;

    /// <summary>Last name.</summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; init; } = string.Empty;

    /// <summary>Email address.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>Egyptian phone number (01XXXXXXXXX).</summary>
    [Phone]
    [RegularExpression(@"^01\d{9}$", ErrorMessage = "Invalid Egyptian phone number.")]
    public string? PhoneNumber { get; init; }
}

/// <summary>Address details DTO.</summary>
public sealed record AddressDto
{
    public int Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Governorate { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string? District { get; init; }
    public string StreetAddress { get; init; } = string.Empty;
    public string? BuildingNo { get; init; }
    public string? ApartmentNo { get; init; }
    public string? PostalCode { get; init; }
    public bool IsDefault { get; init; }
}

/// <summary>Create/update address request.</summary>
public sealed record UpsertAddressRequest
{
    [Required]
    [StringLength(100)]
    public string Label { get; init; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string RecipientName { get; init; } = string.Empty;

    [Required]
    [RegularExpression(@"^01[0125]\d{8}$", ErrorMessage = "Invalid Egyptian phone number.")]
    public string Phone { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Governorate { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; init; } = string.Empty;

    [StringLength(120)]
    public string? District { get; init; }

    [Required]
    [StringLength(300)]
    public string StreetAddress { get; init; } = string.Empty;

    [StringLength(50)]
    public string? BuildingNo { get; init; }

    [StringLength(50)]
    public string? ApartmentNo { get; init; }

    [StringLength(20)]
    public string? PostalCode { get; init; }

    public bool IsDefault { get; init; }
}

/// <summary>Change password request DTO.</summary>
public sealed record ChangePasswordRequest
{
    /// <summary>Current password.</summary>
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>New password.</summary>
    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>New password confirmation.</summary>
    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmNewPassword { get; init; } = string.Empty;
}

/// <summary>Google sign-in request.</summary>
public sealed record GoogleLoginRequest
{
    /// <summary>Google ID token returned by frontend OAuth flow.</summary>
    [Required]
    public string IdToken { get; init; } = string.Empty;
}

/// <summary>Request to send email verification code.</summary>
public sealed record SendEmailVerificationRequest
{
    /// <summary>Optional email override. If empty, current user email is used.</summary>
    [EmailAddress]
    public string? Email { get; init; }
}

/// <summary>Request to verify email confirmation code.</summary>
public sealed record VerifyEmailRequest
{
    /// <summary>Email being verified.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>Email verification code generated by Identity token provider.</summary>
    [Required]
    public string Code { get; init; } = string.Empty;
}

/// <summary>Request to send mobile verification OTP.</summary>
public sealed record SendPhoneOtpRequest
{
    /// <summary>Target phone number in E.164 format (e.g. +2010XXXXXXX).</summary>
    [Required]
    public string PhoneNumber { get; init; } = string.Empty;
}

/// <summary>Request to verify mobile OTP code.</summary>
public sealed record VerifyPhoneOtpRequest
{
    /// <summary>Target phone number in E.164 format.</summary>
    [Required]
    public string PhoneNumber { get; init; } = string.Empty;

    /// <summary>OTP code received via SMS.</summary>
    [Required]
    public string Code { get; init; } = string.Empty;
}

