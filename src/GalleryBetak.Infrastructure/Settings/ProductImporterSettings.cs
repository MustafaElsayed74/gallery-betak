namespace GalleryBetak.Infrastructure.Settings;

/// <summary>
/// Runtime settings for external product import.
/// </summary>
public sealed class ProductImporterSettings
{
    public const string SectionName = "ProductImporter";

    /// <summary>
    /// User-Agent sent to external websites.
    /// </summary>
    public string UserAgent { get; init; } = "GalleryBetakBot/1.0 (+https://gallery-betak.local/importer)";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int RequestTimeoutSeconds { get; init; } = 20;

    /// <summary>
    /// Per-host politeness delay in milliseconds.
    /// </summary>
    public int DelayBetweenRequestsMs { get; init; } = 1200;

    /// <summary>
    /// Maximum number of images to import per product.
    /// </summary>
    public int MaxImages { get; init; } = 6;

    /// <summary>
    /// Maximum downloaded image size in MB.
    /// </summary>
    public int MaxImageSizeMb { get; init; } = 8;

    /// <summary>
    /// Optional domain allowlist. Empty means allow all public hosts.
    /// </summary>
    public List<string> AllowedDomains { get; init; } = [];

    /// <summary>
    /// If true, importer checks robots.txt before fetching product pages.
    /// </summary>
    public bool RespectRobotsTxt { get; init; } = true;
}
