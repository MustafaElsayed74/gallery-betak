namespace ElMasria.Application.DTOs.Auth;

/// <summary>Login request DTO.</summary>
public sealed record LoginRequest
{
    /// <summary>User email.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Password.</summary>
    public string Password { get; init; } = string.Empty;
}

/// <summary>User registration request DTO.</summary>
public sealed record RegisterRequest
{
    /// <summary>First name (supports Arabic).</summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>Last name (supports Arabic).</summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>Email address.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Password (min 8 chars, uppercase, lowercase, digit, special).</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>Password confirmation.</summary>
    public string ConfirmPassword { get; init; } = string.Empty;

    /// <summary>Egyptian phone number (01XXXXXXXXX).</summary>
    public string? PhoneNumber { get; init; }
}

/// <summary>Refresh token request DTO.</summary>
public sealed record RefreshTokenRequest
{
    /// <summary>The expired access token.</summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>The refresh token.</summary>
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

/// <summary>Change password request DTO.</summary>
public sealed record ChangePasswordRequest
{
    /// <summary>Current password.</summary>
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>New password.</summary>
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>New password confirmation.</summary>
    public string ConfirmNewPassword { get; init; } = string.Empty;
}
