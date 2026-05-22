param(
    [string]$CategoryUrl = "https://elghazawy.com/ar/sub-category/kitchen-tools",
    [string]$OutputPath = "src/GalleryBetak.Infrastructure/Data/Seed/elghazawy-kitchen-tools.json",
    [int]$DelayMs = 100
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

function Get-CleanText {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    $decoded = [System.Net.WebUtility]::HtmlDecode($Value)
    return ([regex]::Replace($decoded, "\s+", " ")).Trim()
}

function Get-MetaContent {
    param(
        [string]$Html,
        [string]$Name
    )

    $escaped = [regex]::Escape($Name)
    $patterns = @(
        "<meta[^>]+name\s*=\s*[""']$escaped[""'][^>]+content\s*=\s*[""'](?<value>[^""']*)[""'][^>]*>",
        "<meta[^>]+content\s*=\s*[""'](?<value>[^""']*)[""'][^>]+name\s*=\s*[""']$escaped[""'][^>]*>",
        "<meta[^>]+property\s*=\s*[""']$escaped[""'][^>]+content\s*=\s*[""'](?<value>[^""']*)[""'][^>]*>",
        "<meta[^>]+content\s*=\s*[""'](?<value>[^""']*)[""'][^>]+property\s*=\s*[""']$escaped[""'][^>]*>"
    )

    foreach ($pattern in $patterns) {
        $match = [regex]::Match($Html, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($match.Success) {
            return Get-CleanText $match.Groups["value"].Value
        }
    }

    return $null
}

function ConvertTo-DecimalOrNull {
    param($Value)

    if ($null -eq $Value) {
        return $null
    }

    $text = [string]$Value
    $cleaned = [regex]::Replace($text, "[^0-9.,-]", "")
    if ([string]::IsNullOrWhiteSpace($cleaned)) {
        return $null
    }

    if ($cleaned.Contains(",") -and -not $cleaned.Contains(".")) {
        $cleaned = $cleaned.Replace(",", ".")
    }
    elseif ($cleaned.Contains(",") -and $cleaned.Contains(".")) {
        $cleaned = $cleaned.Replace(",", "")
    }

    $number = 0.0
    if ([decimal]::TryParse($cleaned, [System.Globalization.NumberStyles]::Number, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$number)) {
        return $number
    }

    return $null
}

function Get-ProductJsonLd {
    param([string]$Html)

    $scripts = [regex]::Matches(
        $Html,
        "<script[^>]*type\s*=\s*[""']application/ld\+json[""'][^>]*>(?<json>.*?)</script>",
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Singleline)

    foreach ($script in $scripts) {
        $json = [System.Net.WebUtility]::HtmlDecode($script.Groups["json"].Value).Trim()
        if ([string]::IsNullOrWhiteSpace($json)) {
            continue
        }

        try {
            $node = $json | ConvertFrom-Json
            if ($node.'@type' -match "Product") {
                return $node
            }
        }
        catch {
            continue
        }
    }

    return $null
}

function Get-EnglishNameFromUrl {
    param([string]$Url)

    $uri = [Uri]$Url
    $last = ($uri.AbsolutePath.TrimEnd("/") -split "/")[-1]
    $decoded = [System.Net.WebUtility]::UrlDecode($last.Replace("+", " "))
    return Get-CleanText $decoded
}

function Get-ProductIdFromUrl {
    param([string]$Url)

    $match = [regex]::Match($Url, "/product/(?<id>\d+)/")
    if ($match.Success) {
        return $match.Groups["id"].Value
    }

    return [Guid]::NewGuid().ToString("N").Substring(0, 12)
}

function ConvertFrom-CodePoints {
    param([int[]]$CodePoints)

    return -join ($CodePoints | ForEach-Object { [char]$_ })
}

function Get-ImageUrls {
    param(
        [string]$Html,
        $ProductNode
    )

    $urls = New-Object "System.Collections.Generic.List[string]"

    if ($ProductNode -and $ProductNode.image) {
        foreach ($image in @($ProductNode.image)) {
            if (-not [string]::IsNullOrWhiteSpace([string]$image)) {
                [void]$urls.Add([string]$image)
            }
        }
    }

    $imageMatches = [regex]::Matches($Html, "https://elghazawy\.b-cdn\.net/[^""'<>\s]+?\.webp", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    foreach ($match in $imageMatches) {
        [void]$urls.Add([System.Net.WebUtility]::HtmlDecode($match.Value))
    }

    return @(
        $urls |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and $_.Length -le 500 } |
            Select-Object -Unique |
            Select-Object -First 6
    )
}

$categoryResponse = Invoke-WebRequest -Uri $CategoryUrl -UseBasicParsing -TimeoutSec 60
$categoryHtml = $categoryResponse.Content

$links = [regex]::Matches($categoryHtml, "https://elghazawy\.com/ar/product/[^""'<>\s]+") |
    ForEach-Object { [System.Net.WebUtility]::HtmlDecode($_.Value) } |
    Select-Object -Unique

$products = New-Object "System.Collections.Generic.List[object]"
$index = 0

foreach ($link in $links) {
    $index++
    Write-Host "[$index/$($links.Count)] $link"

    try {
        $response = Invoke-WebRequest -Uri $link -UseBasicParsing -TimeoutSec 60
        $html = $response.Content
        $jsonLd = Get-ProductJsonLd $html

        $productId = Get-ProductIdFromUrl $link
        $nameAr = if ($jsonLd -and $jsonLd.name) { Get-CleanText ([string]$jsonLd.name) } else { Get-CleanText (Get-MetaContent $html "og:title") }
        $nameEn = Get-EnglishNameFromUrl $link
        $descriptionAr = Get-MetaContent $html "description"
        $descriptionEn = $nameEn
        $price = $null
        $originalPrice = $null

        if ($jsonLd -and $jsonLd.offers) {
            $offer = @($jsonLd.offers)[0]
            $price = ConvertTo-DecimalOrNull $offer.price
            if ($offer.highPrice) {
                $originalPrice = ConvertTo-DecimalOrNull $offer.highPrice
            }
        }

        if (-not $price) {
            $price = ConvertTo-DecimalOrNull (Get-MetaContent $html "product:price:amount")
        }

        $images = Get-ImageUrls -Html $html -ProductNode $jsonLd

        if ([string]::IsNullOrWhiteSpace($nameAr) -or -not $price -or $images.Count -eq 0) {
            Write-Warning "Skipped incomplete product: $link"
            continue
        }

        if ($originalPrice -and $originalPrice -le $price) {
            $originalPrice = $null
        }

        [void]$products.Add([PSCustomObject]@{
            sourceId = $productId
            sku = "ELG-$productId"
            nameAr = $nameAr
            nameEn = $nameEn
            descriptionAr = $descriptionAr
            descriptionEn = $descriptionEn
            price = $price
            originalPrice = $originalPrice
            stockQuantity = 30
            material = $null
            origin = "Elghazawy"
            imageUrls = @($images)
            sourceUrl = $link
        })

        if ($DelayMs -gt 0) {
            Start-Sleep -Milliseconds $DelayMs
        }
    }
    catch {
        Write-Warning "Failed $link :: $($_.Exception.Message)"
    }
}

$resolvedOutput = Join-Path (Get-Location) $OutputPath
$outputDirectory = Split-Path $resolvedOutput -Parent
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$payload = [PSCustomObject]@{
    source = $CategoryUrl
    scrapedAtUtc = [DateTime]::UtcNow.ToString("o")
    category = [PSCustomObject]@{
        nameAr = ConvertFrom-CodePoints @(0x0623, 0x062F, 0x0648, 0x0627, 0x062A, 0x0020, 0x0648, 0x0645, 0x0639, 0x062F, 0x0627, 0x062A, 0x0020, 0x0627, 0x0644, 0x0645, 0x0637, 0x0628, 0x062E)
        nameEn = "Kitchen Tools"
        slug = "kitchen-tools"
        descriptionAr = ConvertFrom-CodePoints @(0x0645, 0x0646, 0x062A, 0x062C, 0x0627, 0x062A, 0x0020, 0x0623, 0x062F, 0x0648, 0x0627, 0x062A, 0x0020, 0x0648, 0x0645, 0x0639, 0x062F, 0x0627, 0x062A, 0x0020, 0x0627, 0x0644, 0x0645, 0x0637, 0x0628, 0x062E, 0x0020, 0x0627, 0x0644, 0x0645, 0x0633, 0x062A, 0x0648, 0x0631, 0x062F, 0x0629, 0x0020, 0x0645, 0x0646, 0x0020, 0x0627, 0x0644, 0x063A, 0x0632, 0x0627, 0x0648, 0x064A, 0x002E)
        descriptionEn = "Kitchen tools and equipment imported from Elghazawy."
        imageUrl = "https://elghazawy.com/front_assets/img/logo-large.png"
    }
    products = @($products)
}

$json = $payload | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText($resolvedOutput, $json, [System.Text.Encoding]::UTF8)

"ProductsWritten=$($products.Count)"
"OutputPath=$resolvedOutput"
