param(
    [string]$LocalApiBaseUrl = "http://localhost:5000/api/v1",
    [string]$RemoteApiBaseUrl = "http://gallerybetak.runasp.net/api/v1",
    [string]$AdminEmail = "admin@gallery-betak.com",
    [string]$AdminPassword = "Admin@123456",
    [int]$PageSize = 100,
    [int]$FallbackCategoryId = 1,
    [switch]$ReplaceRemoteCatalog = $true
)

$ErrorActionPreference = "Stop"

function Get-AdminHeaders {
    param(
        [string]$BaseUrl,
        [string]$Email,
        [string]$Password
    )

    $loginBody = @{ email = $Email; password = $Password } | ConvertTo-Json
    $loginResponse = Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/login" -ContentType "application/json" -Body $loginBody

    if (-not $loginResponse.success -or -not $loginResponse.data -or [string]::IsNullOrWhiteSpace($loginResponse.data.accessToken)) {
        throw "Admin login failed for $BaseUrl"
    }

    return @{ Authorization = "Bearer $($loginResponse.data.accessToken)" }
}

function Get-AllProductIds {
    param(
        [string]$BaseUrl,
        [int]$PageSizeValue
    )

    $ids = New-Object "System.Collections.Generic.List[int]"
    $pageNumber = 1

    while ($true) {
        $response = Invoke-RestMethod -Method Get -Uri "$BaseUrl/products?pageNumber=$pageNumber&pageSize=$PageSizeValue"
        $items = @()

        if ($response.data -and $response.data.items) {
            $items = @($response.data.items)
        }

        if ($items.Count -eq 0) {
            break
        }

        foreach ($item in $items) {
            [void]$ids.Add([int]$item.id)
        }

        if ($response.meta -and ($response.meta.hasNext -eq $false)) {
            break
        }

        if ($response.data -and $response.data.totalCount -and $ids.Count -ge [int]$response.data.totalCount) {
            break
        }

        $pageNumber++
    }

    return @($ids)
}

function Get-ProductDetail {
    param(
        [string]$BaseUrl,
        [int]$ProductId
    )

    $response = Invoke-RestMethod -Method Get -Uri "$BaseUrl/products/$ProductId"
    if (-not $response.success -or -not $response.data) {
        throw "Unable to read product detail for ID $ProductId from $BaseUrl"
    }

    return $response.data
}

function Get-CategorySlugMap {
    param([string]$BaseUrl)

    $map = @{}
    $response = Invoke-RestMethod -Method Get -Uri "$BaseUrl/categories"
    $categories = @()

    if ($response.data) {
        $categories = @($response.data)
    }

    foreach ($category in $categories) {
        if (-not [string]::IsNullOrWhiteSpace($category.slug)) {
            $map[$category.slug.ToLowerInvariant()] = [int]$category.id
        }
    }

    return $map
}

function Clear-RemoteProducts {
    param(
        [string]$BaseUrl,
        [hashtable]$Headers
    )

    $deleted = 0

    while ($true) {
        $response = Invoke-RestMethod -Method Get -Uri "$BaseUrl/products?pageNumber=1&pageSize=200" -Headers $Headers
        $items = @()

        if ($response.data -and $response.data.items) {
            $items = @($response.data.items)
        }

        if ($items.Count -eq 0) {
            break
        }

        foreach ($item in $items) {
            $delResponse = Invoke-RestMethod -Method Delete -Uri "$BaseUrl/products/$($item.id)" -Headers $Headers
            if ($delResponse.success) {
                $deleted++
            }
        }
    }

    return $deleted
}

function Invoke-CreateProduct {
    param(
        [string]$BaseUrl,
        [hashtable]$Headers,
        [hashtable]$Body
    )

    $jsonBody = $Body | ConvertTo-Json -Depth 8

    try {
        $response = Invoke-RestMethod -Method Post -Uri "$BaseUrl/products" -Headers $Headers -ContentType "application/json" -Body $jsonBody
        return [PSCustomObject]@{
            Success = [bool]($response.success)
            Status  = if ($response.statusCode) { [int]$response.statusCode } else { 0 }
            Error   = ""
        }
    }
    catch {
        $statusCode = 0
        $errorBody = ""

        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errorBody = $reader.ReadToEnd()
            }
            catch {
                $errorBody = $_.Exception.Message
            }
        }
        else {
            $errorBody = $_.Exception.Message
        }

        return [PSCustomObject]@{
            Success = $false
            Status  = $statusCode
            Error   = $errorBody
        }
    }
}

function Build-CreateRequest {
    param(
        [psobject]$Detail,
        [hashtable]$RemoteCategoryMap,
        [int]$DefaultCategoryId
    )

    $nameAr = if ([string]::IsNullOrWhiteSpace($Detail.nameAr)) { $Detail.nameEn } else { $Detail.nameAr }
    $nameEn = if ([string]::IsNullOrWhiteSpace($Detail.nameEn)) { $nameAr } else { $Detail.nameEn }

    $categoryId = $DefaultCategoryId
    if ($Detail.category -and $Detail.category.slug) {
        $slugKey = $Detail.category.slug.ToLowerInvariant()
        if ($RemoteCategoryMap.ContainsKey($slugKey)) {
            $categoryId = [int]$RemoteCategoryMap[$slugKey]
        }
        elseif ($Detail.category.id) {
            $categoryId = [int]$Detail.category.id
        }
    }

    $imageUrls = @()
    if ($Detail.images) {
        $imageUrls = @(
            $Detail.images |
            Sort-Object displayOrder |
            ForEach-Object { $_.imageUrl } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Select-Object -Unique
        )
    }

    $tagIds = @()
    if ($Detail.tags) {
        $tagIds = @(
            $Detail.tags |
            ForEach-Object { [int]$_.id } |
            Select-Object -Unique
        )
    }

    $sku = if ([string]::IsNullOrWhiteSpace($Detail.sku)) {
        "MIG-$($Detail.id)-$([Guid]::NewGuid().ToString('N').Substring(0, 6))"
    }
    else {
        [string]$Detail.sku
    }

    if ($sku.Length -gt 50) {
        $sku = $sku.Substring(0, 50)
    }

    $originalPrice = $null
    if ($Detail.originalPrice -and [decimal]$Detail.originalPrice -gt [decimal]$Detail.price) {
        $originalPrice = [decimal]$Detail.originalPrice
    }

    return @{
        nameAr        = [string]$nameAr
        nameEn        = [string]$nameEn
        descriptionAr = $Detail.descriptionAr
        descriptionEn = $Detail.descriptionEn
        price         = [decimal]$Detail.price
        originalPrice = $originalPrice
        sku           = $sku
        stockQuantity = [int]([Math]::Max(0, [int]$Detail.stockQuantity))
        categoryId    = [int]$categoryId
        weight        = if ($Detail.weight) { [decimal]$Detail.weight } else { $null }
        dimensions    = $Detail.dimensions
        material      = $Detail.material
        origin        = $Detail.origin
        isFeatured    = [bool]$Detail.isFeatured
        tagIds        = @($tagIds)
        imageUrls     = @($imageUrls)
        sourceUrl     = $null
        importedAt    = [DateTime]::UtcNow.ToString("o")
    }
}

$remoteHeaders = Get-AdminHeaders -BaseUrl $RemoteApiBaseUrl -Email $AdminEmail -Password $AdminPassword
$remoteCategoryMap = Get-CategorySlugMap -BaseUrl $RemoteApiBaseUrl

$deletedRemote = 0
if ($ReplaceRemoteCatalog) {
    $deletedRemote = Clear-RemoteProducts -BaseUrl $RemoteApiBaseUrl -Headers $remoteHeaders
}

$localProductIds = Get-AllProductIds -BaseUrl $LocalApiBaseUrl -PageSizeValue $PageSize
$created = 0
$failed = 0
$failedIds = New-Object "System.Collections.Generic.List[string]"

foreach ($localId in $localProductIds) {
    $detail = Get-ProductDetail -BaseUrl $LocalApiBaseUrl -ProductId $localId
    $createBody = Build-CreateRequest -Detail $detail -RemoteCategoryMap $remoteCategoryMap -DefaultCategoryId $FallbackCategoryId

    $createResult = Invoke-CreateProduct -BaseUrl $RemoteApiBaseUrl -Headers $remoteHeaders -Body $createBody

    if (-not $createResult.Success -and $createResult.Status -eq 409) {
        $suffix = "-" + [Guid]::NewGuid().ToString("N").Substring(0, 4)
        $maxBaseLength = 50 - $suffix.Length
        if ($createBody.sku.Length -gt $maxBaseLength) {
            $createBody.sku = $createBody.sku.Substring(0, $maxBaseLength) + $suffix
        }
        else {
            $createBody.sku = $createBody.sku + $suffix
        }

        $createResult = Invoke-CreateProduct -BaseUrl $RemoteApiBaseUrl -Headers $remoteHeaders -Body $createBody
    }

    if ($createResult.Success) {
        $created++
    }
    else {
        $failed++
        $failedIds.Add("$localId (status=$($createResult.Status))")
    }
}

$remoteFinal = Invoke-RestMethod -Method Get -Uri "$RemoteApiBaseUrl/products?pageNumber=1&pageSize=1"

"LocalProducts=$($localProductIds.Count)"
"RemoteDeleted=$deletedRemote"
"RemoteCreated=$created"
"RemoteFailed=$failed"
"RemoteFinalCount=$($remoteFinal.data.totalCount)"

if ($failedIds.Count -gt 0) {
    "FailedProductIds:"
    $failedIds | Select-Object -First 20
}
