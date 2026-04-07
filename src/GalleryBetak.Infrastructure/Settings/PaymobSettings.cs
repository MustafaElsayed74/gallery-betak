namespace GalleryBetak.Infrastructure.Settings;

/// <summary>
/// Paymob Gateway Configuration bindings.
/// </summary>
public sealed class PaymobSettings
{
    public const string SectionName = "Paymob";

    public string ApiKey { get; init; } = string.Empty;
    public string HmacSecret { get; init; } = string.Empty;
    
    // Integration IDs
    public int CardIntegrationId { get; init; }
    public int WalletIntegrationId { get; init; }
    public int FawryIntegrationId { get; init; }

    // IFrames
    public int CardIframeId { get; init; }
}

