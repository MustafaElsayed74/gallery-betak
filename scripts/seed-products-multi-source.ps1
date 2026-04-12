param(
    [string]$ApiBaseUrl = "http://localhost:5000/api/v1",
    [int]$TargetProducts = 30,
    [int]$PreferredCategoryId = 1,
    [string[]]$SourceUrls = @(
        "https://www.raneen.com/ar/home?p=2",
        "https://elghazawy.com/ar/sub-category/kitchen-tools"
    )
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

function Get-AdminHeaders {
    param([string]$BaseUrl)

    $payload = @{ email = "admin@gallery-betak.com"; password = "Admin@123456" } | ConvertTo-Json
    $login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/login" -ContentType "application/json" -Body $payload

    if (-not $login.success -or -not $login.data.accessToken) {
        throw "Admin login failed."
    }

    return @{ Authorization = "Bearer $($login.data.accessToken)" }
}

function Clear-AllProducts {
    param(
        [string]$BaseUrl,
        [hashtable]$Headers
    )

    $deleted = 0
    while ($true) {
        $list = Invoke-RestMethod -Method Get -Uri "$BaseUrl/products?pageNumber=1&pageSize=200" -Headers $Headers
        $items = @()

        if ($list.data -and $list.data.items) {
            $items = @($list.data.items)
        }

        if ($items.Count -eq 0) {
            break
        }

        foreach ($item in $items) {
            $del = Invoke-RestMethod -Method Delete -Uri "$BaseUrl/products/$($item.id)" -Headers $Headers
            if ($del.success) {
                $deleted++
            }
        }
    }

    return $deleted
}

function Get-RaneenProductLinks {
    param([string]$Url)

    $html = Invoke-WebRequest -Uri $Url -TimeoutSec 45
    $matches = [regex]::Matches($html.Content, 'href\s*=\s*["''](?<u>[^"''#]+)["'']')
    $links = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($m in $matches) {
        $raw = $m.Groups["u"].Value
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
        if ($path -notmatch "^/ar/[^/]+$") {
            continue
        }

        if ($path -match "^/ar/(home|for-men|for-women|brand|customer|checkout|wishlist|storecredit|rma|sales|search|catalogsearch|appliances|electronics|mobiles|kitchen|furniture|family-products|marketplace|help-center|deals|faq|page)(/|$)") {
            continue
        }

        [void]$links.Add("https://www.raneen.com$path")
    }

    return @($links)
}

function Get-ElghazawyProductLinks {
    param([string]$Url)

    $html = Invoke-WebRequest -Uri $Url -TimeoutSec 45
    $matches = [regex]::Matches($html.Content, 'href\s*=\s*["''](?<u>[^"''#]+)["'']')
    $links = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($m in $matches) {
        $raw = $m.Groups["u"].Value
        if ([string]::IsNullOrWhiteSpace($raw)) {
            continue
        }

        $candidate = $null
        if ($raw.StartsWith("http", [System.StringComparison]::OrdinalIgnoreCase)) {
            $candidate = $raw
        }
        elseif ($raw.StartsWith("/", [System.StringComparison]::Ordinal)) {
            $candidate = "https://elghazawy.com$raw"
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

        if (-not $uri.Host.Equals("elghazawy.com", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $path = $uri.AbsolutePath.TrimEnd("/")
        if ($path -match "^/ar/product/\d+/.+$") {
            [void]$links.Add("https://elghazawy.com$path")
        }
    }

    return @($links)
}

function Get-SourceCandidates {
    param([string[]]$Urls)

    $all = New-Object "System.Collections.Generic.List[string]"

    foreach ($url in $Urls) {
        if ($url -match "raneen\.com") {
            (Get-RaneenProductLinks -Url $url) | ForEach-Object { [void]$all.Add($_) }
            continue
        }

        if ($url -match "elghazawy\.com") {
            (Get-ElghazawyProductLinks -Url $url) | ForEach-Object { [void]$all.Add($_) }
            continue
        }
    }

    return @($all | Select-Object -Unique)
}

function Import-UntilTarget {
    param(
        [string]$BaseUrl,
        [hashtable]$Headers,
        [string[]]$Candidates,
        [int]$PreferredCategoryId,
        [int]$Target
    )

    $created = 0
    $skipped = 0
    $failures = New-Object "System.Collections.Generic.List[string]"

    foreach ($link in $Candidates) {
        if ($created -ge $Target) {
            break
        }

        try {
            $importBody = @{ url = $link; preferredCategoryId = $PreferredCategoryId } | ConvertTo-Json
            $previewResp = Invoke-RestMethod -Method Post -Uri "$BaseUrl/admin/products/import" -Headers $Headers -ContentType "application/json" -Body $importBody

            if (-not $previewResp.success -or -not $previewResp.data) {
                $skipped++
                $failures.Add("Import failed: $link")
                continue
            }

            $preview = $previewResp.data
            $price = [decimal]$preview.price
            if ($price -le 0 -or [string]::IsNullOrWhiteSpace($preview.nameAr)) {
                $skipped++
                $failures.Add("Invalid preview: $link")
                continue
            }

            if (-not (Test-ProductRelevance -Preview $preview)) {
                $skipped++
                $failures.Add("Irrelevant preview: $link")
                continue
            }

            if (-not $preview.imageUrls -or @($preview.imageUrls).Count -eq 0) {
                $skipped++
                $failures.Add("Missing images: $link")
                continue
            }

            $categoryId = $PreferredCategoryId
            if ($preview.suggestedCategoryId -and [int]$preview.suggestedCategoryId -gt 0) {
                $categoryId = [int]$preview.suggestedCategoryId
            }

            $sku = "$($preview.suggestedSku)-$([Guid]::NewGuid().ToString('N').Substring(0,4))"

            $createBody = @{
                nameAr        = $preview.nameAr
                nameEn        = $(if ([string]::IsNullOrWhiteSpace($preview.nameEn)) { $preview.nameAr } else { $preview.nameEn })
                descriptionAr = $preview.descriptionAr
                descriptionEn = $preview.descriptionEn
                price         = $price
                originalPrice = $(if ($preview.originalPrice -and [decimal]$preview.originalPrice -gt $price) { [decimal]$preview.originalPrice } else { $null })
                sku           = $sku
                stockQuantity = 30
                categoryId    = $categoryId
                weight        = $preview.weight
                dimensions    = $preview.dimensions
                material      = $preview.material
                origin        = $preview.origin
                isFeatured    = $false
                tagIds        = @()
                imageUrls     = $(if ($preview.imageUrls) { @($preview.imageUrls) } else { @() })
                sourceUrl     = $preview.sourceUrl
                importedAt    = [DateTime]::UtcNow.ToString("o")
            } | ConvertTo-Json -Depth 6

            $createResp = Invoke-RestMethod -Method Post -Uri "$BaseUrl/products" -Headers $Headers -ContentType "application/json" -Body $createBody

            if ($createResp.success) {
                $created++
            }
            else {
                $skipped++
                $failures.Add("Create failed: $link")
            }
        }
        catch {
            $skipped++
            $failures.Add("Error for $link :: $($_.Exception.Message)")
        }
    }

    return [PSCustomObject]@{
        Created  = $created
        Skipped  = $skipped
        Failures = $failures
    }
}

$headers = Get-AdminHeaders -BaseUrl $ApiBaseUrl
$deleted = Clear-AllProducts -BaseUrl $ApiBaseUrl -Headers $headers
$candidates = Get-SourceCandidates -Urls $SourceUrls
$result = Import-UntilTarget -BaseUrl $ApiBaseUrl -Headers $headers -Candidates $candidates -PreferredCategoryId $PreferredCategoryId -Target $TargetProducts
$final = Invoke-RestMethod -Method Get -Uri "$ApiBaseUrl/products?pageNumber=1&pageSize=1" -Headers $headers

"TargetProducts=$TargetProducts"
"DeletedExisting=$deleted"
"CandidateLinks=$($candidates.Count)"
"Created=$($result.Created)"
"Skipped=$($result.Skipped)"
"FinalCatalogCount=$($final.data.totalCount)"

if ($result.Failures.Count -gt 0) {
    "FailuresSample:"
    $result.Failures | Select-Object -First 10
}
