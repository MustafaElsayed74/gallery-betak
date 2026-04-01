using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Auth;
using ElMasria.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElMasria.API.Controllers;

/// <summary>
/// Base API controller with common attributes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>Gets the current user's ID from JWT claims.</summary>
    protected string? CurrentUserId =>
        User.FindFirst("sub")?.Value;
}

/// <summary>
/// Authentication endpoints: login, register, refresh, logout, profile, change password.
/// </summary>
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>Initializes AuthController.</summary>
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates user and returns JWT + refresh token.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>Auth response with tokens and user profile.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid credentials.</response>
    /// <response code="423">Account locked out.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">Registration details.</param>
    /// <returns>Auth response with tokens and user profile.</returns>
    /// <response code="201">User registered successfully.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">Email already exists.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// Implements token rotation (old refresh token is invalidated).
    /// </summary>
    /// <param name="request">Expired access token + valid refresh token.</param>
    /// <returns>New auth response with new token pair.</returns>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="401">Invalid or expired tokens.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Logs out the current user by revoking their refresh token.
    /// </summary>
    /// <returns>Success confirmation.</returns>
    /// <response code="200">Logout successful.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail(401, "غير مصرح", "Unauthorized."));

        var result = await _authService.RevokeTokenAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets the current user's profile.
    /// </summary>
    /// <returns>User profile with roles.</returns>
    /// <response code="200">Profile retrieved.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail(401, "غير مصرح", "Unauthorized."));

        var result = await _authService.GetProfileAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Changes the current user's password. Revokes refresh tokens on all devices.
    /// </summary>
    /// <param name="request">Current and new password.</param>
    /// <returns>Success confirmation.</returns>
    /// <response code="200">Password changed.</response>
    /// <response code="400">Validation failed or incorrect current password.</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail(401, "غير مصرح", "Unauthorized."));

        var result = await _authService.ChangePasswordAsync(userId, request);
        return StatusCode(result.StatusCode, result);
    }
}
