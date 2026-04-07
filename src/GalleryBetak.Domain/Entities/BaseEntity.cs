using Microsoft.AspNetCore.Identity;

namespace GalleryBetak.Domain.Entities;

/// <summary>
/// Base entity with common audit fields. All domain entities inherit from this.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; protected set; }

    /// <summary>UTC timestamp when the entity was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>UTC timestamp of last modification.</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Soft delete flag.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>UTC timestamp when soft deleted.</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>User ID of the creator.</summary>
    public string? CreatedBy { get; private set; }

    /// <summary>Sets the creation timestamp. Called by DbContext on save.</summary>
    public void SetCreatedAt(DateTime utcNow) => CreatedAt = utcNow;

    /// <summary>Sets the update timestamp. Called by DbContext on save.</summary>
    public void SetUpdatedAt(DateTime utcNow) => UpdatedAt = utcNow;

    /// <summary>Sets the creator user ID.</summary>
    public void SetCreatedBy(string userId) => CreatedBy = userId;

    /// <summary>Marks the entity as soft-deleted.</summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    /// <summary>Restores a soft-deleted entity.</summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }
}

/// <summary>
/// Extended ASP.NET Identity user with profile fields and refresh token support.
/// Uses DDD patterns: private setters, factory method, domain methods.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>User's first name (supports Arabic).</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>User's last name (supports Arabic).</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Profile image URL.</summary>
    public string? ProfileImageUrl { get; private set; }

    /// <summary>Hashed refresh token (SHA-256).</summary>
    public string? RefreshToken { get; private set; }

    /// <summary>Refresh token expiry time (UTC).</summary>
    public DateTime? RefreshTokenExpiryTime { get; private set; }

    /// <summary>Whether the user account is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Last login timestamp (UTC).</summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>Account creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>Last update timestamp (UTC).</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Soft delete flag.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>Soft delete timestamp (UTC).</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>Full name computed from first and last name.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    // Protected parameterless constructor for EF Core
    protected ApplicationUser() { }

    /// <summary>
    /// Factory method to create a new user with validated input.
    /// </summary>
    public static ApplicationUser Create(string email, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new Exceptions.DomainException("البريد الإلكتروني مطلوب", "Email is required.");
        if (string.IsNullOrWhiteSpace(firstName))
            throw new Exceptions.DomainException("الاسم الأول مطلوب", "First name is required.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new Exceptions.DomainException("الاسم الأخير مطلوب", "Last name is required.");

        return new ApplicationUser
        {
            UserName = email.ToLowerInvariant(),
            Email = email.ToLowerInvariant(),
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>Updates the user's profile information.</summary>
    public void UpdateProfile(string firstName, string lastName, string? profileImageUrl)
    {
        FirstName = firstName;
        LastName = lastName;
        ProfileImageUrl = profileImageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Sets a hashed refresh token with expiry.</summary>
    public void SetRefreshToken(string hashedToken, DateTime expiryUtc)
    {
        RefreshToken = hashedToken;
        RefreshTokenExpiryTime = expiryUtc;
    }

    /// <summary>Records a successful login.</summary>
    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    /// <summary>Clears the refresh token (logout).</summary>
    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiryTime = null;
    }

    /// <summary>Deactivates the user account.</summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Reactivates the user account.</summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Soft deletes the user.</summary>
    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        IsActive = false;
    }
}

