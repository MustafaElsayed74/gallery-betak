namespace ElMasria.Application.Interfaces;

/// <summary>
/// Provides access to the current authenticated user's claims.
/// Implemented in the Infrastructure/API layer using HttpContext.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The authenticated user's ID. Null if not authenticated.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// The authenticated user's email address.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Whether the current user has Admin or SuperAdmin role.
    /// </summary>
    bool IsAdmin { get; }
}
