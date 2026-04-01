using System.Security.Claims;
using ElMasria.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ElMasria.Infrastructure.Identity;

/// <summary>
/// Extracts the current authenticated user's claims from HttpContext.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="CurrentUserService"/>.
    /// </summary>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public string? UserId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <inheritdoc/>
    public string? Email =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    /// <inheritdoc/>
    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc/>
    public bool IsAdmin =>
        _httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true ||
        _httpContextAccessor.HttpContext?.User.IsInRole("SuperAdmin") == true;
}
