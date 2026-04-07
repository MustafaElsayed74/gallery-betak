using System.Security.Claims;
using GalleryBetak.Domain.Entities;

namespace GalleryBetak.Application.Interfaces;

/// <summary>
/// JWT token generation and validation service interface.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token with user claims and roles.
    /// </summary>
    (string Token, DateTime ExpiresAt) GenerateAccessToken(ApplicationUser user, IList<string> roles);

    /// <summary>
    /// Validates an expired access token and extracts claims.
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

