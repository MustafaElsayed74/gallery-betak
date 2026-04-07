using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Product;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GalleryBetak.Infrastructure.Services;

/// <summary>
/// Server-side importer that fetches product pages, extracts preview fields, and uploads images.
/// </summary>
public sealed class ProductImportService : IProductImportService
{
    private const string ImportHttpClientName = "ProductImporter";

    private static readonly ConcurrentDictionary<string, DateTimeOffset> LastRequestByHost =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> HostLocks =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPhotoService _photoService;
    private readonly ProductImportHtmlParser _htmlParser;
    private readonly ProductImporterSettings _settings;
    private readonly ILogger<ProductImportService> _logger;
    private readonly bool _imageUploadEnabled;

    /// <summary>Initializes importer service dependencies.</summary>
    public ProductImportService(
        IHttpClientFactory httpClientFactory,
        IPhotoService photoService,
        ProductImportHtmlParser htmlParser,
        IOptions<ProductImporterSettings> settings,
        IConfiguration configuration,
        ILogger<ProductImportService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _photoService = photoService;
        _htmlParser = htmlParser;
        _settings = settings.Value;
        _logger = logger;

        var cloudName = configuration["CloudinarySettings:CloudName"];
        var apiKey = configuration["CloudinarySettings:ApiKey"];
        var apiSecret = configuration["CloudinarySettings:ApiSecret"];
        _imageUploadEnabled = !string.IsNullOrWhiteSpace(cloudName)
            && !string.IsNullOrWhiteSpace(apiKey)
            && !string.IsNullOrWhiteSpace(apiSecret);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<ProductImportPreviewDto>> ImportFromUrlAsync(ProductImportRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return ApiResponse<ProductImportPreviewDto>.Fail(
                400,
                "رابط المنتج مطلوب",
                "Product URL is required.");
        }

        if (!TryNormalizeHttpUrl(request.Url, out var sourceUri))
        {
            return ApiResponse<ProductImportPreviewDto>.Fail(
                400,
                "رابط غير صالح. يسمح فقط بروابط HTTP/HTTPS العامة",
                "Invalid URL. Only public HTTP/HTTPS URLs are allowed.");
        }

        if (!IsDomainAllowed(sourceUri.Host))
        {
            return ApiResponse<ProductImportPreviewDto>.Fail(
                403,
                "هذا النطاق غير مسموح للاستيراد",
                "This domain is not allowed for import.");
        }

        var hostIsSafe = await IsPublicHostAsync(sourceUri);
        if (!hostIsSafe)
        {
            return ApiResponse<ProductImportPreviewDto>.Fail(
                400,
                "العنوان الهدف غير مسموح لأسباب أمنية",
                "Target host is not allowed for security reasons.");
        }

        if (_settings.RespectRobotsTxt)
        {
            var allowedByRobots = await IsAllowedByRobotsAsync(sourceUri, ct);
            if (!allowedByRobots)
            {
                return ApiResponse<ProductImportPreviewDto>.Fail(
                    403,
                    "تم رفض الاستيراد وفق سياسة robots.txt",
                    "Import denied by robots.txt policy.");
            }
        }

        await EnforcePoliteDelayAsync(sourceUri.Host, ct);

        var pageFetch = await FetchHtmlAsync(sourceUri, ct);
        if (!pageFetch.Success)
        {
            return ApiResponse<ProductImportPreviewDto>.Fail(
                pageFetch.StatusCode,
                pageFetch.MessageAr,
                pageFetch.MessageEn);
        }

        var extracted = _htmlParser.Extract(pageFetch.Html!, sourceUri);
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(extracted.Name))
        {
            warnings.Add("Could not confidently extract product name. Please review before saving.");
        }

        if (!extracted.Price.HasValue || extracted.Price <= 0)
        {
            warnings.Add("Could not confidently extract product price. Please set the correct price manually.");
        }

        if (!string.IsNullOrWhiteSpace(extracted.Currency)
            && !string.Equals(extracted.Currency, "EGP", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"Detected source currency '{extracted.Currency}'. Verify and convert to EGP before saving.");
        }

        var suggestedSku = BuildSuggestedSku(extracted.Sku, extracted.Name, sourceUri.Host);
        var importedImageUrls = await ImportImagesAsync(extracted.ImageUrls, suggestedSku, warnings, ct);

        if (importedImageUrls.Count == 0)
        {
            warnings.Add("No images were imported. You can still save and add images later.");
        }

        var name = extracted.Name ?? string.Empty;
        var description = extracted.Description;

        var preview = new ProductImportPreviewDto
        {
            SourceUrl = sourceUri.ToString(),
            SourceHost = sourceUri.Host,
            NameAr = name,
            NameEn = name,
            DescriptionAr = description,
            DescriptionEn = description,
            Price = extracted.Price ?? 0m,
            OriginalPrice = extracted.OriginalPrice,
            SuggestedSku = suggestedSku,
            SuggestedCategoryId = request.PreferredCategoryId,
            Weight = extracted.Weight,
            Dimensions = extracted.Dimensions,
            Material = extracted.Material,
            Origin = extracted.Origin,
            Currency = extracted.Currency,
            ImageUrls = importedImageUrls,
            Warnings = warnings
        };

        _logger.LogInformation(
            "Product import preview generated for {SourceUrl}. ExtractedImages={ExtractedCount}, UploadedImages={UploadedCount}",
            sourceUri,
            extracted.ImageUrls.Count,
            importedImageUrls.Count);

        return ApiResponse<ProductImportPreviewDto>.Ok(
            preview,
            "تم تجهيز معاينة الاستيراد بنجاح",
            "Import preview generated successfully.");
    }

    private async Task<ImportFetchResult> FetchHtmlAsync(Uri sourceUri, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(ImportHttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, sourceUri);
            request.Headers.UserAgent.ParseAdd(_settings.UserAgent);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                return ImportFetchResult.Fail(
                    (int)response.StatusCode,
                    "تعذر جلب الصفحة المصدر",
                    "Unable to fetch source page.");
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!string.IsNullOrWhiteSpace(mediaType)
                && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                return ImportFetchResult.Fail(
                    400,
                    "الرابط لا يشير إلى صفحة HTML",
                    "URL does not point to an HTML page.");
            }

            var html = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(html))
            {
                return ImportFetchResult.Fail(
                    400,
                    "الصفحة المصدر فارغة",
                    "Source page is empty.");
            }

            return ImportFetchResult.Ok(html);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Timed out fetching source page: {SourceUrl}", sourceUri);
            return ImportFetchResult.Fail(
                504,
                "انتهت مهلة الاتصال أثناء جلب الصفحة المصدر",
                "Source website timed out while fetching the page.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Product import fetch failed for {SourceUrl}", sourceUri);
            return ImportFetchResult.Fail(
                502,
                "حدث خطأ أثناء جلب الصفحة المصدر",
                "An error occurred while fetching the source page.");
        }
    }

    private async Task<IReadOnlyList<string>> ImportImagesAsync(
        IReadOnlyList<string> imageUrls,
        string suggestedSku,
        List<string> warnings,
        CancellationToken ct)
    {
        if (imageUrls.Count == 0)
        {
            return [];
        }

        var maxBytes = Math.Max(1, _settings.MaxImageSizeMb) * 1024 * 1024;
        var importedUrls = new List<string>();

        var candidates = imageUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, _settings.MaxImages))
            .ToList();

        var client = _httpClientFactory.CreateClient(ImportHttpClientName);

        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (!TryNormalizeHttpUrl(candidate, out var imageUri))
            {
                continue;
            }

            var hostIsSafe = await IsPublicHostAsync(imageUri);
            if (!hostIsSafe)
            {
                warnings.Add($"Skipped unsafe image host: {imageUri.Host}");
                continue;
            }

            try
            {
                await EnforcePoliteDelayAsync(imageUri.Host, ct);

                if (!_imageUploadEnabled)
                {
                    importedUrls.Add(imageUri.ToString());
                    continue;
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, imageUri);
                request.Headers.UserAgent.ParseAdd(_settings.UserAgent);

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!response.IsSuccessStatusCode)
                {
                    warnings.Add($"Failed to download image ({(int)response.StatusCode}): {imageUri}");
                    continue;
                }

                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (string.IsNullOrWhiteSpace(contentType)
                    || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add($"Skipped non-image content: {imageUri}");
                    continue;
                }

                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength.HasValue && contentLength.Value > maxBytes)
                {
                    warnings.Add($"Skipped image larger than {_settings.MaxImageSizeMb} MB: {imageUri}");
                    continue;
                }

                await using var imageStream = await response.Content.ReadAsStreamAsync(ct);
                await using var boundedStream = await CopyToBoundedMemoryAsync(imageStream, maxBytes, ct);
                if (boundedStream is null)
                {
                    warnings.Add($"Skipped image larger than {_settings.MaxImageSizeMb} MB: {imageUri}");
                    continue;
                }

                var extension = DetermineExtension(imageUri, contentType);
                var safeSku = Regex.Replace(suggestedSku, "[^A-Za-z0-9_-]", string.Empty);
                if (string.IsNullOrWhiteSpace(safeSku))
                {
                    safeSku = "imported";
                }

                var fileName = $"{safeSku}-{i + 1}{extension}";
                var uploaded = await _photoService.UploadImageAsync(boundedStream, fileName, "imports");

                if (!string.IsNullOrWhiteSpace(uploaded.Url))
                {
                    importedUrls.Add(uploaded.Url);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed importing image {ImageUrl}", imageUri);
                warnings.Add($"Failed to import image: {imageUri}");
            }
        }

        if (!_imageUploadEnabled && importedUrls.Count > 0)
        {
            warnings.Add("Cloud image upload is not configured. Source image URLs were used directly.");
        }

        return importedUrls;
    }

    private async Task<bool> IsAllowedByRobotsAsync(Uri sourceUri, CancellationToken ct)
    {
        try
        {
            var robotsUri = new Uri($"{sourceUri.Scheme}://{sourceUri.Host}/robots.txt");
            var client = _httpClientFactory.CreateClient(ImportHttpClientName);

            using var request = new HttpRequestMessage(HttpMethod.Get, robotsUri);
            request.Headers.UserAgent.ParseAdd(_settings.UserAgent);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return true;
            }

            if (!response.IsSuccessStatusCode)
            {
                return true;
            }

            var robotsContent = await response.Content.ReadAsStringAsync(ct);
            return IsPathAllowedByRobots(robotsContent, sourceUri.AbsolutePath, GetUserAgentToken(_settings.UserAgent));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogDebug("robots.txt check timed out for {Host}; defaulting to allow", sourceUri.Host);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "robots.txt check failed for {Host}; defaulting to allow", sourceUri.Host);
            return true;
        }
    }

    private async Task EnforcePoliteDelayAsync(string host, CancellationToken ct)
    {
        var delayMs = Math.Max(0, _settings.DelayBetweenRequestsMs);
        if (delayMs == 0)
        {
            return;
        }

        var hostLock = HostLocks.GetOrAdd(host, _ => new SemaphoreSlim(1, 1));
        await hostLock.WaitAsync(ct);
        try
        {
            if (LastRequestByHost.TryGetValue(host, out var lastRequestAt))
            {
                var elapsed = DateTimeOffset.UtcNow - lastRequestAt;
                var remaining = TimeSpan.FromMilliseconds(delayMs) - elapsed;
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, ct);
                }
            }

            LastRequestByHost[host] = DateTimeOffset.UtcNow;
        }
        finally
        {
            hostLock.Release();
        }
    }

    private static async Task<MemoryStream?> CopyToBoundedMemoryAsync(Stream source, int maxBytes, CancellationToken ct)
    {
        var buffer = new byte[81_920];
        var totalRead = 0;
        var memory = new MemoryStream();

        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
            if (totalRead > maxBytes)
            {
                await memory.DisposeAsync();
                return null;
            }

            await memory.WriteAsync(buffer.AsMemory(0, read), ct);
        }

        memory.Position = 0;
        return memory;
    }

    private static string DetermineExtension(Uri imageUri, string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            var extension = contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(extension))
            {
                return extension;
            }
        }

        var pathExtension = Path.GetExtension(imageUri.AbsolutePath);
        if (!string.IsNullOrWhiteSpace(pathExtension)
            && pathExtension.Length <= 5
            && Regex.IsMatch(pathExtension, "^\\.[A-Za-z0-9]+$"))
        {
            return pathExtension.ToLowerInvariant();
        }

        return ".jpg";
    }

    private bool IsDomainAllowed(string host)
    {
        if (_settings.AllowedDomains.Count == 0)
        {
            return true;
        }

        return _settings.AllowedDomains.Any(allowedDomain =>
            host.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith($".{allowedDomain}", StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryNormalizeHttpUrl(string rawUrl, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    private static async Task<bool> IsPublicHostAsync(Uri uri)
    {
        if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IPAddress.TryParse(uri.Host, out var ipAddress))
        {
            return !IsPrivateIpAddress(ipAddress);
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost);
            if (addresses.Length == 0)
            {
                return false;
            }

            return addresses.All(address => !IsPrivateIpAddress(address));
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPrivateIpAddress(IPAddress ipAddress)
    {
        if (IPAddress.IsLoopback(ipAddress))
        {
            return true;
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ipAddress.GetAddressBytes();
            return bytes[0] switch
            {
                10 => true,
                127 => true,
                169 when bytes[1] == 254 => true,
                172 when bytes[1] is >= 16 and <= 31 => true,
                192 when bytes[1] == 168 => true,
                _ => false
            };
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ipAddress.Equals(IPAddress.IPv6Loopback)
                || ipAddress.IsIPv6LinkLocal
                || ipAddress.IsIPv6SiteLocal
                || ipAddress.IsIPv6Multicast)
            {
                return true;
            }

            var bytes = ipAddress.GetAddressBytes();
            // Unique local addresses: fc00::/7
            if ((bytes[0] & 0xFE) == 0xFC)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPathAllowedByRobots(string robotsContent, string path, string userAgentToken)
    {
        var rules = new List<(bool IsAllow, string Pattern)>();
        var activeBlock = false;
        var normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;

        foreach (var rawLine in robotsContent.Split('\n'))
        {
            var line = rawLine.Split('#')[0].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
            {
                var value = line["User-agent:".Length..].Trim();
                activeBlock = value == "*"
                    || value.Contains(userAgentToken, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!activeBlock)
            {
                continue;
            }

            if (line.StartsWith("Allow:", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = line["Allow:".Length..].Trim();
                rules.Add((true, pattern));
            }
            else if (line.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = line["Disallow:".Length..].Trim();
                rules.Add((false, pattern));
            }
        }

        var bestMatch = rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Pattern)
                && normalizedPath.StartsWith(rule.Pattern, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(rule => rule.Pattern.Length)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(bestMatch.Pattern))
        {
            return true;
        }

        return bestMatch.IsAllow;
    }

    private static string GetUserAgentToken(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "*";
        }

        var token = userAgent.Trim();
        var slashIndex = token.IndexOf('/');
        if (slashIndex > 0)
        {
            token = token[..slashIndex];
        }

        return token.Trim();
    }

    private static string BuildSuggestedSku(string? extractedSku, string? name, string host)
    {
        var seed = !string.IsNullOrWhiteSpace(extractedSku)
            ? extractedSku
            : !string.IsNullOrWhiteSpace(name)
                ? name
                : host;

        var normalized = Regex.Replace(seed, "[^A-Za-z0-9_-]", string.Empty).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "IMPORTED";
        }

        if (normalized.Length > 20)
        {
            normalized = normalized[..20];
        }

        return $"{normalized}-{DateTime.UtcNow:MMddHHmm}";
    }

    private sealed record ImportFetchResult(bool Success, int StatusCode, string MessageAr, string MessageEn, string? Html)
    {
        public static ImportFetchResult Ok(string html) => new(true, 200, string.Empty, string.Empty, html);

        public static ImportFetchResult Fail(int statusCode, string messageAr, string messageEn) =>
            new(false, statusCode, messageAr, messageEn, null);
    }
}
