using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Auth;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace GalleryBetak.Infrastructure.Identity;

/// <summary>
/// Authentication service implementation using ASP.NET Identity + JWT.
/// Handles login, registration, token refresh, and password management.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    /// <summary>Initializes AuthService with required dependencies.</summary>
    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
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
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return ApiResponse<AuthResponse>.Fail(400,
                "كلمتا المرور غير متطابقتين",
                "Passwords do not match.");
        }

        // Check if email already exists
        var normalizedEmail = request.Email.Trim();
        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
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
    public async Task<ApiResponse<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var clientId = _configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return ApiResponse<AuthResponse>.Fail(500,
                "Google authentication غير مفعّل",
                "Google authentication is not configured.");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid Google ID token.");
            return ApiResponse<AuthResponse>.Fail(401,
                "رمز Google غير صالح",
                "Invalid Google token.");
        }

        if (!payload.EmailVerified)
        {
            return ApiResponse<AuthResponse>.Fail(401,
                "البريد الإلكتروني من Google غير موثّق",
                "Google email is not verified.");
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            var firstName = string.IsNullOrWhiteSpace(payload.GivenName) ? "Google" : payload.GivenName.Trim();
            var lastName = string.IsNullOrWhiteSpace(payload.FamilyName) ? "User" : payload.FamilyName.Trim();

            user = ApplicationUser.Create(normalizedEmail, firstName, lastName);
            user.EmailConfirmed = true;

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors
                    .Select(e => new ApiError { Field = e.Code, Message = e.Description })
                    .ToList();

                return ApiResponse<AuthResponse>.Fail(400,
                    "فشل إنشاء حساب Google",
                    "Failed to create Google account.",
                    errors);
            }

            await _userManager.AddToRoleAsync(user, "Customer");
        }

        if (user.IsDeleted)
        {
            return ApiResponse<AuthResponse>.Fail(403,
                "الحساب غير متاح",
                "Account is not available.");
        }

        if (!user.IsActive)
        {
            return ApiResponse<AuthResponse>.Fail(403,
                "تم تعطيل حسابك",
                "Your account has been deactivated.");
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
        }

        user.RecordLogin();
        var authResponse = await GenerateAuthResponseAsync(user);
        await _userManager.UpdateAsync(user);

        return ApiResponse<AuthResponse>.Ok(authResponse,
            "تم تسجيل الدخول عبر Google بنجاح",
            "Google login successful.");
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
            PhoneNumber = user.PhoneNumber,
            FullName = user.FullName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfileImageUrl = user.ProfileImageUrl,
            Roles = roles.ToList().AsReadOnly()
        };

        return ApiResponse<UserProfileDto>.Ok(profile);
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(string userId,
        UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted)
        {
            return ApiResponse<UserProfileDto>.Fail(404, "المستخدم غير موجود", "User not found.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userManager.FindByEmailAsync(normalizedEmail);
            if (existing is not null && existing.Id != user.Id)
            {
                return ApiResponse<UserProfileDto>.Fail(409,
                    "البريد الإلكتروني مستخدم بالفعل",
                    "Email is already in use.");
            }
        }

        user.UpdateProfile(request.FirstName.Trim(), request.LastName.Trim(), user.ProfileImageUrl);
        user.Email = normalizedEmail;
        user.UserName = normalizedEmail;
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors
                .Select(e => new ApiError { Field = e.Code, Message = e.Description })
                .ToList();

            return ApiResponse<UserProfileDto>.Fail(400,
                "فشل تحديث الملف الشخصي",
                "Failed to update profile.",
                errors);
        }

        return await GetProfileAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<IReadOnlyList<AddressDto>>> GetAddressesAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        var addresses = await _unitOfWork.Addresses.FindAsync(
            a => a.UserId == userId && !a.IsDeleted,
            cancellationToken);

        var mapped = addresses
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(MapAddress)
            .ToList()
            .AsReadOnly();

        return ApiResponse<IReadOnlyList<AddressDto>>.Ok(mapped);
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<AddressDto>> CreateAddressAsync(string userId,
        UpsertAddressRequest request, CancellationToken cancellationToken = default)
    {
        var existingAddresses = await _unitOfWork.Addresses.FindAsync(
            a => a.UserId == userId && !a.IsDeleted,
            cancellationToken);

        var makeDefault = request.IsDefault || existingAddresses.Count == 0;
        if (makeDefault)
        {
            foreach (var existing in existingAddresses.Where(a => a.IsDefault))
            {
                existing.ClearDefault();
                _unitOfWork.Addresses.Update(existing);
            }
        }

        var address = Address.Create(
            userId,
            request.Label.Trim(),
            request.RecipientName.Trim(),
            request.Phone.Trim(),
            request.Governorate.Trim(),
            request.City.Trim(),
            request.StreetAddress.Trim(),
            string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim(),
            string.IsNullOrWhiteSpace(request.BuildingNo) ? null : request.BuildingNo.Trim(),
            string.IsNullOrWhiteSpace(request.ApartmentNo) ? null : request.ApartmentNo.Trim(),
            string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim(),
            makeDefault);

        await _unitOfWork.Addresses.AddAsync(address, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AddressDto>.Created(MapAddress(address),
            "تمت إضافة العنوان بنجاح",
            "Address added successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<AddressDto>> UpdateAddressAsync(string userId, int addressId,
        UpsertAddressRequest request, CancellationToken cancellationToken = default)
    {
        var address = await _unitOfWork.Addresses.GetByIdAsync(addressId, cancellationToken);
        if (address is null || address.IsDeleted || address.UserId != userId)
        {
            return ApiResponse<AddressDto>.Fail(404,
                "العنوان غير موجود",
                "Address not found.");
        }

        address.Update(
            request.Label.Trim(),
            request.RecipientName.Trim(),
            request.Phone.Trim(),
            request.Governorate.Trim(),
            request.City.Trim(),
            request.StreetAddress.Trim(),
            string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim(),
            string.IsNullOrWhiteSpace(request.BuildingNo) ? null : request.BuildingNo.Trim(),
            string.IsNullOrWhiteSpace(request.ApartmentNo) ? null : request.ApartmentNo.Trim(),
            string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim());

        if (request.IsDefault)
        {
            var addresses = await _unitOfWork.Addresses.FindAsync(
                a => a.UserId == userId && !a.IsDeleted,
                cancellationToken);

            foreach (var other in addresses.Where(a => a.Id != addressId && a.IsDefault))
            {
                other.ClearDefault();
                _unitOfWork.Addresses.Update(other);
            }

            address.SetDefault();
        }

        _unitOfWork.Addresses.Update(address);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AddressDto>.Ok(MapAddress(address),
            "تم تحديث العنوان بنجاح",
            "Address updated successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> SetDefaultAddressAsync(string userId, int addressId,
        CancellationToken cancellationToken = default)
    {
        var addresses = await _unitOfWork.Addresses.FindAsync(
            a => a.UserId == userId && !a.IsDeleted,
            cancellationToken);

        var target = addresses.FirstOrDefault(a => a.Id == addressId);
        if (target is null)
        {
            return ApiResponse<bool>.Fail(404,
                "العنوان غير موجود",
                "Address not found.");
        }

        foreach (var address in addresses)
        {
            if (address.Id == addressId)
            {
                address.SetDefault();
            }
            else if (address.IsDefault)
            {
                address.ClearDefault();
            }

            _unitOfWork.Addresses.Update(address);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Ok(true,
            "تم تعيين العنوان الافتراضي",
            "Default address updated.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> DeleteAddressAsync(string userId, int addressId,
        CancellationToken cancellationToken = default)
    {
        var addresses = await _unitOfWork.Addresses.FindAsync(
            a => a.UserId == userId && !a.IsDeleted,
            cancellationToken);

        var target = addresses.FirstOrDefault(a => a.Id == addressId);
        if (target is null)
        {
            return ApiResponse<bool>.Fail(404,
                "العنوان غير موجود",
                "Address not found.");
        }

        var wasDefault = target.IsDefault;
        target.SoftDelete();
        _unitOfWork.Addresses.Update(target);

        if (wasDefault)
        {
            var replacement = addresses
                .Where(a => a.Id != target.Id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault();

            if (replacement is not null)
            {
                replacement.SetDefault();
                _unitOfWork.Addresses.Update(replacement);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Ok(true,
            "تم حذف العنوان",
            "Address deleted.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> SendEmailVerificationAsync(string userId, SendEmailVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted)
        {
            return ApiResponse<bool>.Fail(404, "المستخدم غير موجود", "User not found.");
        }

        var targetEmail = string.IsNullOrWhiteSpace(request.Email)
            ? (user.Email ?? string.Empty)
            : request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(targetEmail))
        {
            return ApiResponse<bool>.Fail(400,
                "لا يوجد بريد إلكتروني صالح",
                "No valid email found for verification.");
        }

        if (!string.Equals(user.Email, targetEmail, StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<bool>.Fail(400,
                "يجب التحقق من البريد الإلكتروني المسجل فقط",
                "Only the account email can be verified.");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        try
        {
            await SendVerificationEmailAsync(targetEmail, token, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Email verification configuration is incomplete.");
            return ApiResponse<bool>.Fail(500,
                "خدمة البريد الإلكتروني غير مفعّلة",
                "Email verification is not configured.");
        }

        return ApiResponse<bool>.Ok(true,
            "تم إرسال كود التحقق إلى البريد الإلكتروني",
            "Email verification code sent.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> VerifyEmailAsync(string userId, VerifyEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted)
        {
            return ApiResponse<bool>.Fail(404, "المستخدم غير موجود", "User not found.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<bool>.Fail(400,
                "البريد الإلكتروني غير مطابق للحساب",
                "Email does not match current account.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Code.Trim());
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(e => new ApiError { Field = e.Code, Message = e.Description })
                .ToList();

            return ApiResponse<bool>.Fail(400,
                "كود التحقق غير صالح أو منتهي",
                "Invalid or expired verification code.",
                errors);
        }

        return ApiResponse<bool>.Ok(true,
            "تم تأكيد البريد الإلكتروني بنجاح",
            "Email verified successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> SendPhoneOtpAsync(string userId, SendPhoneOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted)
        {
            return ApiResponse<bool>.Fail(404, "المستخدم غير موجود", "User not found.");
        }

        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
        var verifySid = _configuration["Twilio:VerifyServiceSid"];
        var accountSid = _configuration["Twilio:AccountSID"];
        var authToken = _configuration["Twilio:AuthToken"];

        if (string.IsNullOrWhiteSpace(verifySid) || string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
        {
            return ApiResponse<bool>.Fail(500,
                "Twilio verification غير مفعّل",
                "Twilio verification is not configured.");
        }

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await client.PostAsync(
            $"https://verify.twilio.com/v2/Services/{verifySid}/Verifications",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = normalizedPhone,
                ["Channel"] = "sms"
            }),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Twilio send OTP failed: {Status} {Body}", response.StatusCode, body);

            return ApiResponse<bool>.Fail(400,
                "فشل إرسال رمز التحقق",
                "Failed to send OTP code.");
        }

        return ApiResponse<bool>.Ok(true,
            "تم إرسال رمز التحقق إلى الهاتف",
            "OTP sent successfully.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<bool>> VerifyPhoneOtpAsync(string userId, VerifyPhoneOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted)
        {
            return ApiResponse<bool>.Fail(404, "المستخدم غير موجود", "User not found.");
        }

        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
        var verifySid = _configuration["Twilio:VerifyServiceSid"];
        var accountSid = _configuration["Twilio:AccountSID"];
        var authToken = _configuration["Twilio:AuthToken"];

        if (string.IsNullOrWhiteSpace(verifySid) || string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
        {
            return ApiResponse<bool>.Fail(500,
                "Twilio verification غير مفعّل",
                "Twilio verification is not configured.");
        }

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await client.PostAsync(
            $"https://verify.twilio.com/v2/Services/{verifySid}/VerificationCheck",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = normalizedPhone,
                ["Code"] = request.Code.Trim()
            }),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Twilio verify OTP failed: {Status} {Body}", response.StatusCode, body);
            return ApiResponse<bool>.Fail(400,
                "رمز التحقق غير صالح",
                "Invalid verification code.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var status = doc.RootElement.TryGetProperty("status", out var statusElement)
            ? statusElement.GetString()
            : null;

        if (!string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<bool>.Fail(400,
                "رمز التحقق غير صالح أو منتهي",
                "Invalid or expired verification code.");
        }

        user.PhoneNumber = normalizedPhone;
        user.PhoneNumberConfirmed = true;
        await _userManager.UpdateAsync(user);

        return ApiResponse<bool>.Ok(true,
            "تم توثيق رقم الهاتف بنجاح",
            "Phone number verified successfully.");
    }

    // ── Private Helpers ───────────────────────────────────────────

    private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(user, roles);

        // Generate and store hashed refresh token
        var refreshToken = JwtTokenService.GenerateRefreshToken();
        var refreshExpiryDays = int.Parse(
            _configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "30");
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
                PhoneNumber = user.PhoneNumber,
                FullName = user.FullName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileImageUrl = user.ProfileImageUrl,
                Roles = roles.ToList().AsReadOnly()
            }
        };
    }

    private static AddressDto MapAddress(Address address)
    {
        return new AddressDto
        {
            Id = address.Id,
            Label = address.Label,
            RecipientName = address.RecipientName,
            Phone = address.Phone,
            Governorate = address.Governorate,
            City = address.City,
            District = address.District,
            StreetAddress = address.StreetAddress,
            BuildingNo = address.BuildingNo,
            ApartmentNo = address.ApartmentNo,
            PostalCode = address.PostalCode,
            IsDefault = address.IsDefault
        };
    }

    private async Task SendVerificationEmailAsync(string targetEmail, string token, CancellationToken cancellationToken)
    {
        var smtpHost = _configuration["EmailConfiguration:SmtpServer"] ?? _configuration["SmtpSettings:Host"];
        var smtpPortRaw = _configuration["EmailConfiguration:Port"] ?? _configuration["SmtpSettings:Port"];
        var username = _configuration["EmailConfiguration:UserName"] ?? _configuration["SmtpSettings:Username"];
        var password = _configuration["EmailConfiguration:Password"] ?? _configuration["SmtpSettings:Password"];
        var from = _configuration["EmailConfiguration:From"] ?? _configuration["SmtpSettings:FromEmail"] ?? username;

        if (string.IsNullOrWhiteSpace(smtpHost) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("Email SMTP configuration is incomplete.");
        }

        var port = int.TryParse(smtpPortRaw, out var parsedPort) ? parsedPort : 587;
        var enableSsl = port == 465;

        using var message = new MailMessage(from, targetEmail)
        {
            Subject = "GalleryBetak Email Verification",
            Body = $"Your verification code is:\n{token}\n\nIf you did not request this, ignore this email.",
            IsBodyHtml = false
        };

        using var smtp = new SmtpClient(smtpHost, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(username, password)
        };

        using var registration = cancellationToken.Register(() => smtp.SendAsyncCancel());
        await smtp.SendMailAsync(message, cancellationToken);
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        var value = phoneNumber.Trim().Replace(" ", string.Empty);
        if (value.StartsWith("00", StringComparison.Ordinal))
        {
            value = "+" + value[2..];
        }

        if (!value.StartsWith("+", StringComparison.Ordinal))
        {
            if (value.StartsWith("0", StringComparison.Ordinal))
            {
                value = "+2" + value;
            }
            else
            {
                value = "+" + value;
            }
        }

        return value;
    }
}

