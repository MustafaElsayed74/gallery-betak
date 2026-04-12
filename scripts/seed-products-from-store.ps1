param(
    [string]$ApiBaseUrl = "http://localhost:5000/api/v1",
    [string]$StoreUrl = "https://www.raneen.com/ar/home",
    [int]$PreferredCategoryId = 1,
    [int]$MaxProducts = 30
)

$ErrorActionPreference = "Stop"

$BlockedKeywords = @(
    "كاميرا", "camera", "هودي", "hoodie", "تي شيرت", "t-shirt", "shirt", "ملابس", "fashion",
    "موضة", "موبايل", "mobile", "phone", "laptop", "computer", "الكترونيات", "إلكترونيات",
    "electronics", "tablet", "تابلت", "security", "surveillance", "مراقبة", "كاميرات",
    "هيكفيجن", "hikvision", "nvr", "dvr", "cctv", "ان في ار", "إن في ار",
    "إسدال", "سيدات",
    "women", "women's", "ladies", "dress", "skirt", "مَرتبة", "مرتبة", "mattress",
    "bed", "blanket", "duvet", "coverlet", "pillow", "sheet", "rug", "carpet",
    "sofa", "couch", "chair", "wardrobe", "dresser", "curtain", "bedding", "furniture"
)

function Test-ProductRelevance {
    param([object]$Preview)

    $text = @(
        $Preview.nameAr,
        $Preview.nameEn,
        $Preview.descriptionAr,
        $Preview.descriptionEn,
        $Preview.material,
        $Preview.origin
    ) -join ' '

    if ([string]::IsNullOrWhiteSpace($text)) {
        return $false
    }

    foreach ($term in $BlockedKeywords) {
        if ($text.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $false
        }
    }

    return $true
}

function Get-AdminToken {
    param([string]$BaseUrl)

    $loginPayload = @{
        email    = "admin@gallery-betak.com"
        password = "Admin@123456"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/login" -ContentType "application/json" -Body $loginPayload

    if (-not $loginResponse.success -or -not $loginResponse.data.accessToken) {
        throw "Admin login failed."
    }

    return $loginResponse.data.accessToken
}

function Clear-ExistingProducts {
    param(
        [string]$BaseUrl,
        [hashtable]$Headers
    )

    $deleted = 0

    while ($true) {
        $productsResponse = Invoke-RestMethod -Method Get -Uri "$BaseUrl/products?pageNumber=1&pageSize=200" -Headers $Headers
        $items = @()

        if ($productsResponse.data -and $productsResponse.data.items) {
            $items = @($productsResponse.data.items)
        }

        if ($items.Count -eq 0) {
            break
        }

        foreach ($item in $items) {
            $deleteResponse = Invoke-RestMethod -Method Delete -Uri "$BaseUrl/products/$($item.id)" -Headers $Headers
            if ($deleteResponse.success) {
                $deleted++
            }
        }
    }

    return $deleted
}

function Get-StoreProductLinks {
    param(
        [string]$HomepageUrl,
        [int]$Limit
    )

    $page = Invoke-WebRequest -Uri $HomepageUrl -TimeoutSec 45
    $matches = [regex]::Matches($page.Content, 'href\s*=\s*["''](?<url>[^"''#]+)["'']')

    $excludedSlugs = @(
        "",
        "home",
        "appliances",
        "electronics",
        "mobiles",
        "kitchen",
        "furniture",
        "family-products",
        "wishlist",
        "checkout",
        "sales",
        "search",
        "catalogsearch",
        "help-center",
        "privacy-policy-cookie-restriction-mode",
        "sitemap.html",
        "deals"
    )

    $unique = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($match in $matches) {
        $raw = $match.Groups["url"].Value
        if ([string]::IsNullOrWhiteSpace($raw)) {
            continue
        }

        $candidate = $null

        if ($raw.StartsWith("http", [System.StringComparison]::OrdinalIgnoreCase)) {
            $candidate = $raw
        }
        elseif ($raw.StartsWith("/", [System.StringComparison]::Ordinal)) {
            $candidate = "https://www.raneen.com$raw"
        }
        else {
            continue
        }

        try {
            $uri = [Uri]$candidate
        }
        catch {
            continue
        }

        if (-not $uri.Host.Equals("www.raneen.com", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $path = $uri.AbsolutePath.TrimEnd("/")
        if (-not $path.StartsWith("/ar/", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        if ($path -notmatch "^/ar/[^/]+$") {
            continue
        }

        $slug = $path.Substring(4)
        if ($excludedSlugs -contains $slug) {
            continue
        }

        if ($slug.StartsWith("brand", [System.StringComparison]::OrdinalIgnoreCase) -or
            $slug.StartsWith("customer", [System.StringComparison]::OrdinalIgnoreCase) -or
            $slug.StartsWith("marketplace", [System.StringComparison]::OrdinalIgnoreCase) -or
            $slug.StartsWith("storecredit", [System.StringComparison]::OrdinalIgnoreCase) -or
            $slug.StartsWith("rma", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $normalized = "https://www.raneen.com$path"
        [void]$unique.Add($normalized)

        if ($unique.Count -ge $Limit) {
            break
        }
    }

    return @($unique)
}

function Import-Products {
    param(
        [string]$BaseUrl,
        [hashtable]$Headers,
        [string[]]$ProductLinks,
        [int]$FallbackCategoryId
    )

    $created = 0
    $skipped = 0
    $failed = @()

    foreach ($link in $ProductLinks) {
        try {
            $importPayload = @{
                url                 = $link
                preferredCategoryId = $FallbackCategoryId
            } | ConvertTo-Json

            $importResponse = Invoke-RestMethod -Method Post -Uri "$BaseUrl/admin/products/import" -Headers $Headers -ContentType "application/json" -Body $importPayload

            if (-not $importResponse.success -or -not $importResponse.data) {
                $skipped++
                $failed += "Import failed: $link"
                continue
            }

            $preview = $importResponse.data
            $price = [decimal]$preview.price

            if ($price -le 0 -or [string]::IsNullOrWhiteSpace($preview.nameAr)) {
                $skipped++
                $failed += "Invalid preview data: $link"
                continue
            }

            if (-not (Test-ProductRelevance -Preview $preview)) {
                $skipped++
                $failed += "Irrelevant preview data: $link"
                continue
            }

            if (-not $preview.imageUrls -or @($preview.imageUrls).Count -eq 0) {
                $skipped++
                $failed += "Missing images: $link"
                continue
            }

            $categoryId = $FallbackCategoryId
            if ($preview.suggestedCategoryId -and [int]$preview.suggestedCategoryId -gt 0) {
                $categoryId = [int]$preview.suggestedCategoryId
            }

            $originalPrice = $null
            if ($preview.originalPrice -and [decimal]$preview.originalPrice -gt $price) {
                $originalPrice = [decimal]$preview.originalPrice
            }

            $createPayload = @{
                nameAr        = $preview.nameAr
                nameEn        = if ([string]::IsNullOrWhiteSpace($preview.nameEn)) { $preview.nameAr } else { $preview.nameEn }
                descriptionAr = $preview.descriptionAr
                descriptionEn = $preview.descriptionEn
                price         = $price
                originalPrice = $originalPrice
                sku           = $preview.suggestedSku
                stockQuantity = 25
                categoryId    = $categoryId
                weight        = $preview.weight
                dimensions    = $preview.dimensions
                material      = $preview.material
                origin        = $preview.origin
                isFeatured    = $false
                tagIds        = @()
                imageUrls     = if ($preview.imageUrls) { @($preview.imageUrls) } else { @() }
                sourceUrl     = $preview.sourceUrl
                importedAt    = [DateTime]::UtcNow.ToString("o")
            } | ConvertTo-Json -Depth 6

            $createResponse = Invoke-RestMethod -Method Post -Uri "$BaseUrl/products" -Headers $Headers -ContentType "application/json" -Body $createPayload

            if ($createResponse.success) {
                $created++
            }
            else {
                $skipped++
                $failed += "Create failed: $link"
            }
        }
        catch {
            $skipped++
            $failed += "Error for $link : $($_.Exception.Message)"
        }
    }

    return [PSCustomObject]@{
        Created  = $created
        Skipped  = $skipped
        Failures = $failed
    }
}

$token = Get-AdminToken -BaseUrl $ApiBaseUrl
$headers = @{ Authorization = "Bearer $token" }

$deletedCount = Clear-ExistingProducts -BaseUrl $ApiBaseUrl -Headers $headers
$links = Get-StoreProductLinks -HomepageUrl $StoreUrl -Limit $MaxProducts
$importResult = Import-Products -BaseUrl $ApiBaseUrl -Headers $headers -ProductLinks $links -FallbackCategoryId $PreferredCategoryId
$finalCountResponse = Invoke-RestMethod -Method Get -Uri "$ApiBaseUrl/products?pageNumber=1&pageSize=1"

"StoreUrl=$StoreUrl"
"DiscoveredLinks=$($links.Count)"
"DeletedExisting=$deletedCount"
"ImportedCreated=$($importResult.Created)"
"ImportedSkipped=$($importResult.Skipped)"
"FinalCatalogCount=$($finalCountResponse.data.totalCount)"

if ($importResult.Failures.Count -gt 0) {
    "Failures:"
    $importResult.Failures | Select-Object -First 20
}
