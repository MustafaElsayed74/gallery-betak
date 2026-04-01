using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Auth;
using ElMasria.Application.Interfaces;
using ElMasria.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ElMasria.Infrastructure.Identity;

/// <summary>
/// Authentication service implementation using ASP.NET Identity + JWT.
/// Handles login, registration, token refresh, and password management.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    /// <summary>Initializes AuthService with required dependencies.</summary>
    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || user.IsDeleted)
        {
            _logger.LogWarning("Login attempt for non-existent email: {Email}", request.Email);
            return ApiResponse<AuthResponse>.Fail(401,
                "البريد الإلكتروني أو كلمة المرور غير صحيحة",
                "Invalid email or password.");
        }

        if (!user.IsActive)
        {
            return ApiResponse<AuthResponse>.Fail(403,
                "تم تعطيل حسابك. يرجى التواصل مع الدعم",
                "Your account has been deactivated.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account locked out: {Email}", request.Email);
            return ApiResponse<AuthResponse>.Fail(423,
                "تم قفل حسابك مؤقتاً بسبب محاولات دخول متعددة. حاول بعد 15 دقيقة",
                "Account locked due to multiple failed attempts. Try again in 15 minutes.");
        }

        if (!result.Succeeded)
        {
            return ApiResponse<AuthResponse>.Fail(401,
                "البريد الإلكتروني أو كلمة المرور غير صحيحة",
                "Invalid email or password.");
        }

        // Generate tokens
        var authResponse = await GenerateAuthResponseAsync(user);
        user.RecordLogin();
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Email} logged in successfully", user.Email);
        return ApiResponse<AuthResponse>.Ok(authResponse, "تم تسجيل الدخول بنجاح", "Login successful.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return ApiResponse<AuthResponse>.Fail(409,
                "البريد الإلكتروني مسجل بالفعل",
                "Email already registered.");
        }

        // Create user using domain factory method
        var user = ApplicationUser.Create(request.Email, request.FirstName, request.LastName);

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            user.PhoneNumber = request.PhoneNumber;
        }

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(e => new ApiError { Field = e.Code, Message = e.Description })
                .ToList();

            return ApiResponse<AuthResponse>.Fail(400,
                "فشل إنشاء الحساب",
                "Account creation failed.",
                errors);
        }

        // Assign default role
        await _userManager.AddToRoleAsync(user, "Customer");

        // Generate tokens
        var authResponse = await GenerateAuthResponseAsync(user);
        user.RecordLogin();
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("New user registered: {Email}", user.Email);
        return ApiResponse<AuthResponse>.Created(authResponse,
            "تم إنشاء الحساب بنجاح",
            "Account created successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate the expired access token structure
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
        {
            return ApiResponse<AuthResponse>.Fail(401,
                "رمز الوصول غير صالح",
                "Invalid access token.");
        }

        var userId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<AuthResponse>.Fail(401,
                "رمز الوصول غير صالح",
                "Invalid access token.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            return ApiResponse<AuthResponse>.Fail(401,
                "المستخدم غير موجود أو غير نشط",
                "User not found or inactive.");
        }

        // Validate refresh token (compare hashed)
        var hashedIncoming = JwtTokenService.HashRefreshToken(request.RefreshToken);
        if (user.RefreshToken != hashedIncoming)
        {
            _logger.LogWarning("Invalid refresh token attempt for user: {UserId}", userId);
            return ApiResponse<AuthResponse>.Fail(401,
                "رمز التحديث غير صالح",
                "Invalid refresh token.");
        }

        if (user.RefreshTokenExpiryTime is null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            return ApiResponse<AuthResponse>.Fail(401,
                "رمز التحديث منتهي الصلاحية. يرجى تسجيل الدخول مرة أخرى",
                "Refresh token expired. Please login again.");
        }

        // Generate new token pair (token rotation)
        var authResponse = await GenerateAuthResponseAsync(user);
        await _userManager.UpdateAsync(user);

        return ApiResponse<AuthResponse>.Ok(authResponse,
            "تم تجديد رمز الوصول",
            "Token refreshed successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> RevokeTokenAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ApiResponse<bool>.Fail(404, "المستخدم غير موجود", "User not found.");
        }

        user.RevokeRefreshToken();
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {UserId} logged out (token revoked)", userId);
        return ApiResponse<bool>.Ok(true, "تم تسجيل الخروج بنجاح", "Logged out successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> ChangePasswordAsync(string userId,
        ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ApiResponse<bool>.Fail(404, "المستخدم غير موجود", "User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(e => new ApiError { Field = e.Code, Message = e.Description })
                .ToList();

            return ApiResponse<bool>.Fail(400,
                "فشل تغيير كلمة المرور",
                "Password change failed.",
                errors);
        }

        // Revoke refresh token to force re-login on other devices
        user.RevokeRefreshToken();
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {UserId} changed password", userId);
        return ApiResponse<bool>.Ok(true, "تم تغيير كلمة المرور بنجاح", "Password changed successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted)
        {
            return ApiResponse<UserProfileDto>.Fail(404, "المستخدم غير موجود", "User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        var profile = new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfileImageUrl = user.ProfileImageUrl,
            Roles = roles.ToList().AsReadOnly()
        };

        return ApiResponse<UserProfileDto>.Ok(profile);
    }

    // ── Private Helpers ───────────────────────────────────────────

    private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(user, roles);

        // Generate and store hashed refresh token
        var refreshToken = JwtTokenService.GenerateRefreshToken();
        var refreshExpiryDays = int.Parse(
            _configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");
        user.SetRefreshToken(
            JwtTokenService.HashRefreshToken(refreshToken),
            DateTime.UtcNow.AddDays(refreshExpiryDays));

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileImageUrl = user.ProfileImageUrl,
                Roles = roles.ToList().AsReadOnly()
            }
        };
    }
}
