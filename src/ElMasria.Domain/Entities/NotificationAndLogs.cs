namespace ElMasria.Domain.Entities;

/// <summary>
/// User notification with bilingual messages and reference linking.
/// </summary>
public sealed class Notification
{
    /// <summary>Primary key.</summary>
    public int Id { get; private set; }

    /// <summary>Target user ID.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Title in Arabic.</summary>
    public string TitleAr { get; private set; } = string.Empty;

    /// <summary>Title in English.</summary>
    public string TitleEn { get; private set; } = string.Empty;

    /// <summary>Message body in Arabic.</summary>
    public string MessageAr { get; private set; } = string.Empty;

    /// <summary>Message body in English.</summary>
    public string MessageEn { get; private set; } = string.Empty;

    /// <summary>Notification type (OrderUpdate, Payment, Promotion, System).</summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>Referenced entity ID (e.g., OrderId).</summary>
    public int? ReferenceId { get; private set; }

    /// <summary>Referenced entity type (e.g., "Order").</summary>
    public string? ReferenceType { get; private set; }

    /// <summary>Whether the notification has been read.</summary>
    public bool IsRead { get; private set; }

    /// <summary>When the notification was read.</summary>
    public DateTime? ReadAt { get; private set; }

    /// <summary>Creation timestamp.</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Notification() { }

    /// <summary>Creates a notification.</summary>
    public static Notification Create(string userId, string titleAr, string titleEn,
        string messageAr, string messageEn, string type,
        int? referenceId = null, string? referenceType = null)
    {
        return new Notification
        {
            UserId = userId,
            TitleAr = titleAr,
            TitleEn = titleEn,
            MessageAr = messageAr,
            MessageEn = messageEn,
            Type = type,
            ReferenceId = referenceId,
            ReferenceType = referenceType
        };
    }

    /// <summary>Marks as read.</summary>
    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Immutable audit log entry. No update or delete operations.
/// Tracks all admin/system actions for compliance.
/// </summary>
public sealed class AuditLog : BaseEntity
{
    /// <summary>User who performed the action.</summary>
    public string? UserId { get; private set; }

    /// <summary>User email for quick reference.</summary>
    public string? UserEmail { get; private set; }

    /// <summary>Action performed (e.g., "CreateProduct").</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Entity type (e.g., "Product", "Order").</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>Entity ID.</summary>
    public string? EntityId { get; private set; }

    /// <summary>Old values as JSON (for updates).</summary>
    public string? OldValues { get; private set; }

    /// <summary>New values as JSON.</summary>
    public string? NewValues { get; private set; }

    /// <summary>Client IP address.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Client user agent string.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>Event timestamp.</summary>
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    private AuditLog() { }

    /// <summary>Creates an audit log entry.</summary>
    public static AuditLog Create(string action, string entityType, string? entityId,
        string? userId = null, string? userEmail = null,
        string? oldValues = null, string? newValues = null,
        string? ipAddress = null, string? userAgent = null)
    {
        return new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            UserEmail = userEmail,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }
}

/// <summary>
/// Search query log for analytics (popular searches, zero-result queries).
/// </summary>
public sealed class SearchLog
{
    /// <summary>Primary key (BIGINT).</summary>
    public long Id { get; private set; }

    /// <summary>Search query text.</summary>
    public string Query { get; private set; } = string.Empty;

    /// <summary>Number of results returned.</summary>
    public int ResultCount { get; private set; }

    /// <summary>Searching user ID (null for guests).</summary>
    public string? UserId { get; private set; }

    /// <summary>Search language (ar/en).</summary>
    public string Language { get; private set; } = "ar";

    /// <summary>Search timestamp.</summary>
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    private SearchLog() { }

    /// <summary>Creates a search log entry.</summary>
    public static SearchLog Create(string query, int resultCount, string? userId = null, string language = "ar")
    {
        return new SearchLog
        {
            Query = query,
            ResultCount = resultCount,
            UserId = userId,
            Language = language
        };
    }
}
