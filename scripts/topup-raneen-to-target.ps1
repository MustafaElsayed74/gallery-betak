param(
    [string]$ApiBaseUrl = "http://localhost:5000/api/v1",
    [int]$TargetCount = 30,
    [int]$StartPage = 3,
    [int]$MaxPages = 8,
    [int]$PreferredCategoryId = 1
)

$ErrorActionPreference = "Stop"

function Get-AdminHeaders {
    param([string]$BaseUrl)

    $payload = @{ email = "admin@gallery-betak.com"; password = "Admin@123456" } | ConvertTo-Json
    $login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/login" -ContentType "application/json" -Body $payload

    if (-not $login.success -or -not $login.data.accessToken) {
        throw "Admin login failed."
    }

    return @{ Authorization = "Bearer $($login.data.accessToken)" }
}

function Get-RaneenPageLinks {
    param([int]$PageNumber)

    $url = "https://www.raneen.com/ar/home?p=$PageNumber"
    $html = Invoke-WebRequest -Uri $url -TimeoutSec 45
    $matches = [regex]::Matches($html.Content, 'href\s*=\s*["''](?<u>[^"''#]+)["'']')
    $set = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)

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

        if ($path -match "^/ar/(home|privacy-policy-cookie-restriction-mode|sitemap\.html|help-center|help-center-about-us-6728c02646b44|return-policy|brand|customer|checkout|wishlist|storecredit|rma|sales|search|catalogsearch|appliances|electronics|mobiles|kitchen|furniture|family-products|marketplace|deals|faq|page)(/|$)") {
            continue
        }

        [void]$set.Add("https://www.raneen.com$path")
    }

    return @($set)
}

function Create-FromPreview {
    param(
        [string]$BaseUrl,
        [hashtable]$Headers,
        [object]$Preview,
        [int]$CategoryId
    )

    $sku = "$($Preview.suggestedSku)-$([Guid]::NewGuid().ToString('N').Substring(0,4))"
    $price = [decimal]$Preview.price

    $body = @{
        nameAr        = $Preview.nameAr
        nameEn        = $(if ([string]::IsNullOrWhiteSpace($Preview.nameEn)) { $Preview.nameAr } else { $Preview.nameEn })
        descriptionAr = $Preview.descriptionAr
        descriptionEn = $Preview.descriptionEn
        price         = $price
        originalPrice = $(if ($Preview.originalPrice -and [decimal]$Preview.originalPrice -gt $price) { [decimal]$Preview.originalPrice } else { $null })
        sku           = $sku
        stockQuantity = 30
        categoryId    = $CategoryId
        weight        = $Preview.weight
        dimensions    = $Preview.dimensions
        material      = $Preview.material
        origin        = $Preview.origin
        isFeatured    = $false
        tagIds        = @()
        imageUrls     = $(if ($Preview.imageUrls) { @($Preview.imageUrls) } else { @() })
        sourceUrl     = $Preview.sourceUrl
        importedAt    = [DateTime]::UtcNow.ToString("o")
    } | ConvertTo-Json -Depth 6

    return Invoke-RestMethod -Method Post -Uri "$BaseUrl/products" -Headers $Headers -ContentType "application/json" -Body $body
}

$headers = Get-AdminHeaders -BaseUrl $ApiBaseUrl
$current = [int](Invoke-RestMethod -Method Get -Uri "$ApiBaseUrl/products?pageNumber=1&pageSize=1").data.totalCount
$needed = $TargetCount - $current

if ($needed -le 0) {
    "Before=$current"
    "Needed=0"
    "CreatedNow=0"
    "Final=$current"
    exit 0
}

$created = 0
$skipped = 0
$page = $StartPage
$seen = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)

while ($created -lt $needed -and $page -le $MaxPages) {
    $links = Get-RaneenPageLinks -PageNumber $page

    foreach ($link in $links) {
        if ($created -ge $needed) {
            break
        }

        if (-not $seen.Add($link)) {
            continue
        }

        try {
            $import = Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/admin/products/import" -Headers $headers -ContentType "application/json" -Body (@{ url = $link; preferredCategoryId = $PreferredCategoryId } | ConvertTo-Json)
            if (-not $import.success -or -not $import.data) {
                $skipped++
                continue
            }

            $preview = $import.data
            if ([decimal]$preview.price -le 0 -or [string]::IsNullOrWhiteSpace($preview.nameAr)) {
                $skipped++
                continue
            }

            $categoryId = $PreferredCategoryId
            if ($preview.suggestedCategoryId -and [int]$preview.suggestedCategoryId -gt 0) {
                $categoryId = [int]$preview.suggestedCategoryId
            }

            $createdResponse = Create-FromPreview -BaseUrl $ApiBaseUrl -Headers $headers -Preview $preview -CategoryId $categoryId
            if ($createdResponse.success) {
                $created++
            }
            else {
                $skipped++
            }
        }
        catch {
            $skipped++
        }
    }

    $page++
}

$final = [int](Invoke-RestMethod -Method Get -Uri "$ApiBaseUrl/products?pageNumber=1&pageSize=1").data.totalCount
"Before=$current"
"Needed=$needed"
"CreatedNow=$created"
"SkippedNow=$skipped"
"Final=$final"
