using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GalleryBetak.Infrastructure.Services;

/// <summary>
/// Extracts product-like fields from HTML using JSON-LD and meta tag fallbacks.
/// </summary>
public sealed class ProductImportHtmlParser
{
    private static readonly Regex JsonLdScriptRegex = new(
        "<script[^>]*type\\s*=\\s*[\"']application/ld\\+json[\"'][^>]*>(?<json>.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex MetaTagRegex = new(
        "<meta\\s+[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TitleRegex = new(
        "<title[^>]*>(?<title>.*?)</title>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex ImageTagRegex = new(
        "<img\\s+[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AttributeRegex = new(
        "(?<name>[a-zA-Z_:][a-zA-Z0-9_:\\-]*)\\s*=\\s*[\"'](?<value>[^\"']*)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] ProductImageHints =
    [
        "product", "products", "pdp", "item", "gallery", "zoom", "image", "images", "main", "detail", "original", "large"
    ];

    private static readonly string[] RejectedImageHints =
    [
        "logo", "icon", "favicon", "sprite", "avatar", "placeholder", "badge", "banner",
        "tracking", "pixel", "payment", "footer", "header", "navbar", "nav", "menu",
        "appstore", "playstore", "googleplay", "facebook", "instagram", "twitter", "youtube", "whatsapp"
    ];

    private sealed record ImageCandidate(string Url, int BaseScore, string? Context);

    /// <summary>
    /// Parses a product page and returns extracted values.
    /// </summary>
    public ProductImportExtractionResult Extract(string html, Uri sourceUri)
    {
        var extracted = ExtractFromJsonLd(html, sourceUri) ?? new ProductImportExtractionResult();

        var meta = ExtractMetaDictionary(html);

        extracted.Name ??= GetMetaValue(meta, "og:title")
            ?? GetMetaValue(meta, "twitter:title")
            ?? ExtractTitle(html);

        extracted.Description ??= GetMetaValue(meta, "og:description")
            ?? GetMetaValue(meta, "description")
            ?? GetMetaValue(meta, "twitter:description");

        if (!extracted.Price.HasValue)
        {
            extracted.Price = ParseDecimal(
                GetMetaValue(meta, "product:price:amount")
                ?? GetMetaValue(meta, "price")
                ?? GetMetaValue(meta, "product:price"));
        }

        extracted.Currency ??= GetMetaValue(meta, "product:price:currency")
            ?? GetMetaValue(meta, "currency");

        var allImageCandidates = new List<ImageCandidate>();
        var hasStructuredImages = extracted.ImageUrls.Count > 0;

        allImageCandidates.AddRange(extracted.ImageUrls.Select(url => new ImageCandidate(url, 120, "jsonld product image")));

        var ogImage = GetMetaValue(meta, "og:image")
            ?? GetMetaValue(meta, "twitter:image");

        if (!hasStructuredImages && !string.IsNullOrWhiteSpace(ogImage))
        {
            allImageCandidates.Add(new ImageCandidate(ogImage, 70, "og image"));
        }

        if (!hasStructuredImages)
        {
            allImageCandidates.AddRange(ExtractImageSources(html));
        }
        else
        {
            // When structured images already exist, only allow large DOM candidates as optional supplements.
            allImageCandidates.AddRange(ExtractImageSources(html).Where(candidate => candidate.BaseScore >= 35));
        }

        extracted.ImageUrls = RankAndFilterImageCandidates(allImageCandidates, sourceUri);

        extracted.Name = CleanText(extracted.Name);
        extracted.Description = CleanText(extracted.Description);
        extracted.Sku = CleanText(extracted.Sku);
        extracted.Material = CleanText(extracted.Material);
        extracted.Origin = CleanText(extracted.Origin);
        extracted.Dimensions = CleanText(extracted.Dimensions);

        return extracted;
    }

    private static ProductImportExtractionResult? ExtractFromJsonLd(string html, Uri sourceUri)
    {
        foreach (Match match in JsonLdScriptRegex.Matches(html))
        {
            var json = WebUtility.HtmlDecode(match.Groups["json"].Value).Trim();
            if (string.IsNullOrWhiteSpace(json))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                var productNode = FindProductNode(document.RootElement);
                if (!productNode.HasValue)
                {
                    continue;
                }

                var node = productNode.Value;
                var extraction = new ProductImportExtractionResult
                {
                    Name = GetJsonString(node, "name"),
                    Description = GetJsonString(node, "description"),
                    Sku = GetJsonString(node, "sku")
                };

                ReadOffers(node, extraction);
                ReadImages(node, sourceUri, extraction);
                ReadAdditionalProperties(node, extraction);

                return extraction;
            }
            catch
            {
                // Skip malformed JSON-LD blocks and continue fallbacks.
            }
        }

        return null;
    }

    private static JsonElement? FindProductNode(JsonElement root)
    {
        if (IsProductNode(root))
        {
            return root;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var product = FindProductNode(item);
                if (product.HasValue)
                {
                    return product;
                }
            }
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("@graph", out var graph))
            {
                var product = FindProductNode(graph);
                if (product.HasValue)
                {
                    return product;
                }
            }

            if (root.TryGetProperty("mainEntity", out var mainEntity))
            {
                var product = FindProductNode(mainEntity);
                if (product.HasValue)
                {
                    return product;
                }
            }

            if (root.TryGetProperty("item", out var item))
            {
                var product = FindProductNode(item);
                if (product.HasValue)
                {
                    return product;
                }
            }
        }

        return null;
    }

    private static bool IsProductNode(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty("@type", out var typeElement))
        {
            return false;
        }

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return typeElement.GetString()?.Contains("Product", StringComparison.OrdinalIgnoreCase) == true;
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            return typeElement.EnumerateArray()
                .Any(type => type.GetString()?.Contains("Product", StringComparison.OrdinalIgnoreCase) == true);
        }

        return false;
    }

    private static void ReadOffers(JsonElement productNode, ProductImportExtractionResult extraction)
    {
        if (!productNode.TryGetProperty("offers", out var offers))
        {
            return;
        }

        if (offers.ValueKind == JsonValueKind.Array)
        {
            foreach (var offer in offers.EnumerateArray())
            {
                if (TryReadOffer(offer, extraction))
                {
                    return;
                }
            }

            return;
        }

        TryReadOffer(offers, extraction);
    }

    private static bool TryReadOffer(JsonElement offer, ProductImportExtractionResult extraction)
    {
        if (offer.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        extraction.Price ??= ParseDecimal(GetJsonString(offer, "price"));
        extraction.Currency ??= GetJsonString(offer, "priceCurrency");

        var highPrice = ParseDecimal(GetJsonString(offer, "highPrice"));
        if (highPrice.HasValue && extraction.Price.HasValue && highPrice > extraction.Price)
        {
            extraction.OriginalPrice = highPrice;
        }

        return extraction.Price.HasValue || !string.IsNullOrWhiteSpace(extraction.Currency);
    }

    private static void ReadImages(JsonElement productNode, Uri sourceUri, ProductImportExtractionResult extraction)
    {
        if (!productNode.TryGetProperty("image", out var imageNode))
        {
            return;
        }

        var imageUrls = new List<string>();

        if (imageNode.ValueKind == JsonValueKind.String)
        {
            imageUrls.Add(imageNode.GetString() ?? string.Empty);
        }
        else if (imageNode.ValueKind == JsonValueKind.Array)
        {
            foreach (var image in imageNode.EnumerateArray())
            {
                if (image.ValueKind == JsonValueKind.String)
                {
                    imageUrls.Add(image.GetString() ?? string.Empty);
                }
                else if (image.ValueKind == JsonValueKind.Object)
                {
                    imageUrls.Add(GetJsonString(image, "url") ?? string.Empty);
                }
            }
        }
        else if (imageNode.ValueKind == JsonValueKind.Object)
        {
            imageUrls.Add(GetJsonString(imageNode, "url") ?? string.Empty);
        }

        extraction.ImageUrls = imageUrls
            .Select(image => NormalizeUrl(image, sourceUri))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Where(url => !IsHardExcludedImageUrl(url!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()!;
    }

    private static void ReadAdditionalProperties(JsonElement productNode, ProductImportExtractionResult extraction)
    {
        extraction.Material ??= GetJsonString(productNode, "material");
        extraction.Dimensions ??= GetJsonString(productNode, "size");
        extraction.Weight ??= ParseDecimal(GetJsonString(productNode, "weight"));

        if (!productNode.TryGetProperty("additionalProperty", out var additionalProperty))
        {
            return;
        }

        if (additionalProperty.ValueKind == JsonValueKind.Object)
        {
            TryMapAdditionalProperty(additionalProperty, extraction);
            return;
        }

        if (additionalProperty.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var property in additionalProperty.EnumerateArray())
        {
            TryMapAdditionalProperty(property, extraction);
        }
    }

    private static void TryMapAdditionalProperty(JsonElement property, ProductImportExtractionResult extraction)
    {
        var name = GetJsonString(property, "name");
        var value = GetJsonString(property, "value");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (name.Contains("material", StringComparison.OrdinalIgnoreCase))
        {
            extraction.Material ??= value;
        }
        else if (name.Contains("origin", StringComparison.OrdinalIgnoreCase)
            || name.Contains("country", StringComparison.OrdinalIgnoreCase)
            || name.Contains("made in", StringComparison.OrdinalIgnoreCase))
        {
            extraction.Origin ??= value;
        }
        else if (name.Contains("dimension", StringComparison.OrdinalIgnoreCase)
            || name.Contains("size", StringComparison.OrdinalIgnoreCase))
        {
            extraction.Dimensions ??= value;
        }
        else if (name.Contains("weight", StringComparison.OrdinalIgnoreCase))
        {
            extraction.Weight ??= ParseDecimal(value);
        }
    }

    private static Dictionary<string, string> ExtractMetaDictionary(string html)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match tagMatch in MetaTagRegex.Matches(html))
        {
            var attributes = ParseAttributes(tagMatch.Value);
            if (attributes.Count == 0)
            {
                continue;
            }

            var key = attributes.GetValueOrDefault("property")
                ?? attributes.GetValueOrDefault("name")
                ?? attributes.GetValueOrDefault("itemprop");

            var content = attributes.GetValueOrDefault("content");
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            result[key.Trim()] = WebUtility.HtmlDecode(content.Trim());
        }

        return result;
    }

    private static Dictionary<string, string> ParseAttributes(string tag)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match attributeMatch in AttributeRegex.Matches(tag))
        {
            var key = attributeMatch.Groups["name"].Value;
            var value = attributeMatch.Groups["value"].Value;
            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static IEnumerable<ImageCandidate> ExtractImageSources(string html)
    {
        foreach (Match imageMatch in ImageTagRegex.Matches(html))
        {
            var attributes = ParseAttributes(imageMatch.Value);
            var src = GetImageValueFromAttributes(attributes);

            if (!string.IsNullOrWhiteSpace(src))
            {
                var width = ParsePixelSize(attributes.GetValueOrDefault("width"));
                var height = ParsePixelSize(attributes.GetValueOrDefault("height"));
                var minSide = width.HasValue && height.HasValue
                    ? Math.Min(width.Value, height.Value)
                    : width ?? height;

                var context = string.Join(
                    " ",
                    attributes.GetValueOrDefault("class"),
                    attributes.GetValueOrDefault("id"),
                    attributes.GetValueOrDefault("alt"),
                    attributes.GetValueOrDefault("aria-label"),
                    attributes.GetValueOrDefault("itemprop"));

                var baseScore = 20;
                if (minSide.HasValue && minSide.Value <= 120)
                {
                    baseScore -= 40;
                }
                else if (minSide.HasValue && minSide.Value >= 280)
                {
                    baseScore += 15;
                }

                yield return new ImageCandidate(src, baseScore, context);
            }
        }
    }

    private static List<string> RankAndFilterImageCandidates(IEnumerable<ImageCandidate> candidates, Uri sourceUri)
    {
        var ranked = new List<(string Url, int Score, int Order)>();
        var order = 0;

        foreach (var candidate in candidates)
        {
            var normalizedUrl = NormalizeUrl(candidate.Url, sourceUri);
            if (string.IsNullOrWhiteSpace(normalizedUrl) || IsHardExcludedImageUrl(normalizedUrl))
            {
                order++;
                continue;
            }

            var score = candidate.BaseScore
                + ScoreUrlHeuristics(normalizedUrl, sourceUri)
                + ScoreContextHeuristics(candidate.Context);

            // Keep strong sources (JSON-LD) even with weak hints; filter noisy DOM images.
            if (candidate.BaseScore < 100 && score < 18)
            {
                order++;
                continue;
            }

            ranked.Add((normalizedUrl, score, order));
            order++;
        }

        return ranked
            .GroupBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Order)
                .First())
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Order)
            .Select(item => item.Url)
            .ToList();
    }

    private static int ScoreUrlHeuristics(string normalizedUrl, Uri sourceUri)
    {
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri))
        {
            return -100;
        }

        var score = 0;
        if (uri.Host.Equals(sourceUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            score += 4;
        }

        var haystack = $"{uri.AbsolutePath} {uri.Query}";
        foreach (var hint in ProductImageHints)
        {
            if (ContainsHint(haystack, hint))
            {
                score += 6;
            }
        }

        foreach (var hint in RejectedImageHints)
        {
            if (ContainsHint(haystack, hint))
            {
                score -= 16;
            }
        }

        var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        score += extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".avif" => 4,
            ".gif" => -2,
            ".svg" or ".ico" => -100,
            _ => 0
        };

        return score;
    }

    private static int ScoreContextHeuristics(string? context)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return 0;
        }

        var score = 0;
        foreach (var hint in ProductImageHints)
        {
            if (ContainsHint(context, hint))
            {
                score += 8;
            }
        }

        foreach (var hint in RejectedImageHints)
        {
            if (ContainsHint(context, hint))
            {
                score -= 18;
            }
        }

        return score;
    }

    private static bool IsHardExcludedImageUrl(string url)
    {
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return true;
        }

        var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        if (extension is ".svg" or ".ico")
        {
            return true;
        }

        var haystack = $"{uri.AbsolutePath} {uri.Query}";
        return RejectedImageHints.Any(hint => ContainsHint(haystack, hint));
    }

    private static string? GetImageValueFromAttributes(IReadOnlyDictionary<string, string> attributes)
    {
        var src = attributes.GetValueOrDefault("src")
            ?? attributes.GetValueOrDefault("data-src")
            ?? attributes.GetValueOrDefault("data-original")
            ?? attributes.GetValueOrDefault("data-lazy-src")
            ?? attributes.GetValueOrDefault("data-image");

        if (!string.IsNullOrWhiteSpace(src))
        {
            return src;
        }

        var srcset = attributes.GetValueOrDefault("srcset")
            ?? attributes.GetValueOrDefault("data-srcset");

        if (string.IsNullOrWhiteSpace(srcset))
        {
            return null;
        }

        var candidates = srcset
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault())
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        return candidates.Count == 0 ? null : candidates[^1];
    }

    private static int? ParsePixelSize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var match = Regex.Match(raw, "\\d+");
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size)
            ? size
            : null;
    }

    private static bool ContainsHint(string input, string hint)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(hint))
        {
            return false;
        }

        var index = input.IndexOf(hint, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            var beforeOk = index == 0 || !char.IsLetterOrDigit(input[index - 1]);
            var afterIndex = index + hint.Length;
            var afterOk = afterIndex >= input.Length || !char.IsLetterOrDigit(input[afterIndex]);

            if (beforeOk && afterOk)
            {
                return true;
            }

            index = input.IndexOf(hint, index + 1, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string? ExtractTitle(string html)
    {
        var match = TitleRegex.Match(html);
        if (!match.Success)
        {
            return null;
        }

        return WebUtility.HtmlDecode(match.Groups["title"].Value).Trim();
    }

    private static string? GetMetaValue(IReadOnlyDictionary<string, string> meta, string key)
    {
        return meta.TryGetValue(key, out var value) ? value : null;
    }

    private static string? GetJsonString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.ToString();
        }

        return null;
    }

    private static decimal? ParseDecimal(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var cleaned = Regex.Replace(input, "[^0-9,\\.\\-]", string.Empty);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return null;
        }

        if (cleaned.Contains(',') && !cleaned.Contains('.'))
        {
            cleaned = cleaned.Replace(',', '.');
        }
        else if (cleaned.Contains(',') && cleaned.Contains('.'))
        {
            cleaned = cleaned.Replace(",", string.Empty);
        }

        return decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string? NormalizeUrl(string? url, Uri sourceUri)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var trimmed = WebUtility.HtmlDecode(url.Trim());

        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            trimmed = $"{sourceUri.Scheme}:{trimmed}";
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return absolute.ToString();
        }

        if (Uri.TryCreate(sourceUri, trimmed, out var relative)
            && (relative.Scheme == Uri.UriSchemeHttp || relative.Scheme == Uri.UriSchemeHttps))
        {
            return relative.ToString();
        }

        return null;
    }

    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Regex.Replace(WebUtility.HtmlDecode(value), "\\s+", " ").Trim();
    }
}

/// <summary>
/// Raw extraction model before import normalization.
/// </summary>
public sealed class ProductImportExtractionResult
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? Currency { get; set; }
    public string? Sku { get; set; }
    public decimal? Weight { get; set; }
    public string? Dimensions { get; set; }
    public string? Material { get; set; }
    public string? Origin { get; set; }
    public List<string> ImageUrls { get; set; } = [];
}
