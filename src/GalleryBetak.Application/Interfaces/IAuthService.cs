using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Auth;

namespace GalleryBetak.Application.Interfaces;

/// <summary>
/// Authentication service contract. Implemented in Infrastructure layer
/// using ASP.NET Identity + JWT.
/// </summary>
public interface IAuthService
{
    /// <summary>Authenticates user and returns JWT + refresh token.</summary>
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Registers a new user account.</summary>
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Authenticates or creates user account from Google identity token.</summary>
    Task<ApiResponse<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Refreshes an expired access token using a valid refresh token.</summary>
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Revokes the user's refresh token (logout).</summary>
    Task<ApiResponse<bool>> RevokeTokenAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Changes the user's password.</summary>
    Task<ApiResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets the current user's profile.</summary>
    Task<ApiResponse<UserProfileDto>> GetProfileAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Updates the current user's profile.</summary>
    Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets the current user's addresses.</summary>
    Task<ApiResponse<IReadOnlyList<AddressDto>>> GetAddressesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new address for the current user.</summary>
    Task<ApiResponse<AddressDto>> CreateAddressAsync(string userId, UpsertAddressRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing address for the current user.</summary>
    Task<ApiResponse<AddressDto>> UpdateAddressAsync(string userId, int addressId, UpsertAddressRequest request, CancellationToken cancellationToken = default);

    /// <summary>Marks one address as default for the current user.</summary>
    Task<ApiResponse<bool>> SetDefaultAddressAsync(string userId, int addressId, CancellationToken cancellationToken = default);

    /// <summary>Deletes an address for the current user.</summary>
    Task<ApiResponse<bool>> DeleteAddressAsync(string userId, int addressId, CancellationToken cancellationToken = default);

    /// <summary>Sends email verification code to the user email.</summary>
    Task<ApiResponse<bool>> SendEmailVerificationAsync(string userId, SendEmailVerificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Verifies email confirmation code and confirms user email.</summary>
    Task<ApiResponse<bool>> VerifyEmailAsync(string userId, VerifyEmailRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sends mobile OTP via Twilio Verify.</summary>
    Task<ApiResponse<bool>> SendPhoneOtpAsync(string userId, SendPhoneOtpRequest request, CancellationToken cancellationToken = default);

    /// <summary>Verifies mobile OTP and confirms phone number for the user.</summary>
    Task<ApiResponse<bool>> VerifyPhoneOtpAsync(string userId, VerifyPhoneOtpRequest request, CancellationToken cancellationToken = default);
}

