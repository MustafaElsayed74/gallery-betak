$j = Get-Content 'src/GalleryBetak.Infrastructure/Data/Seed/elghazawy-kitchen-tools.json' -Raw | ConvertFrom-Json
Write-Host "Total products: $($j.products.Count)"
Write-Host ""

# Group products by keywords to understand categorization
$keywords = @{
    "scales" = @("ميزان", "موازين")
    "cookware" = @("حلة", "حلل", "طاسة", "طاسات", "قدر", "كسرولة", "صينية", "فرن", "طهي", "طبخ", "مقلاة", "تيفال")
    "storage" = @("حفظ", "تخزين", "برطمان", "علبة", "حافظة", "ثلاجة")
    "cutlery" = @("سكين", "سكاكين", "شوك", "معالق", "ملعقة", "شوكة")
    "cups" = @("كوب", "أكواب", "مج", "ماج", "كاس", "زجاجة", "ترمس", "شرب")
    "accessories" = @("مبشرة", "فتاحة", "هراسة", "مصفاة", "قطاعة", "خلاط", "عصارة", "مفرمة")
    "tableware" = @("طبق", "أطباق", "صحن", "صحون", "مائدة", "تقديم", "سلطانية", "بولة")
}

foreach ($cat in $keywords.Keys) {
    $count = 0
    foreach ($p in $j.products) {
        foreach ($kw in $keywords[$cat]) {
            if ($p.nameAr -match $kw) {
                $count++
                break
            }
        }
    }
    Write-Host "${cat}: $count products"
}
