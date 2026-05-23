using GalleryBetak.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace GalleryBetak.Infrastructure.Data
{
    public static class AppDbContextSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Check if the new category structure is already seeded by checking for one of the new root categories
            if (await context.Categories.AnyAsync(c => c.NameAr == "أدوات المطبخ" && c.ParentId == null))
            {
                return;
            }

            // Read the embedded JSON resource
            var assembly = Assembly.GetExecutingAssembly();
            string jsonContent = string.Empty;
            
            using (var stream = assembly.GetManifestResourceStream("GalleryBetak.Infrastructure.Data.Seed.elghazawy-kitchen-tools.json"))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("Could not find embedded resource 'GalleryBetak.Infrastructure.Data.Seed.elghazawy-kitchen-tools.json'");
                }
                using (var reader = new StreamReader(stream))
                {
                    jsonContent = await reader.ReadToEndAsync();
                }
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var payload = JsonSerializer.Deserialize<SeedPayload>(jsonContent, options);
            if (payload == null || payload.Category == null || payload.Products == null)
            {
                return;
            }

            // 1. Delete ProductTags and ProductImages for ELG-% and legacy SKUs (SKU001 - SKU010)
            var legacySkus = Enumerable.Range(1, 10).Select(i => $"SKU{i:D3}").ToList();

            var productsToDelete = await context.Products
                .Where(p => p.SKU.StartsWith("ELG-") || legacySkus.Contains(p.SKU))
                .ToListAsync();

            if (productsToDelete.Any())
            {
                var productIds = productsToDelete.Select(p => p.Id).ToList();

                // Delete ProductImages
                var imagesToDelete = await context.ProductImages
                    .Where(pi => productIds.Contains(pi.ProductId))
                    .ToListAsync();
                context.ProductImages.RemoveRange(imagesToDelete);

                // Delete ProductTags if any
                var tagsToDelete = await context.ProductTags
                    .Where(pt => productIds.Contains(pt.ProductId))
                    .ToListAsync();
                context.ProductTags.RemoveRange(tagsToDelete);

                // Delete related CartItems (ON DELETE RESTRICT prevention)
                var cartItemsToDelete = await context.CartItems
                    .Where(ci => productIds.Contains(ci.ProductId))
                    .ToListAsync();
                context.CartItems.RemoveRange(cartItemsToDelete);

                // Delete related WishlistItems (ON DELETE RESTRICT prevention)
                var wishlistItemsToDelete = await context.WishlistItems
                    .Where(wi => productIds.Contains(wi.ProductId))
                    .ToListAsync();
                context.WishlistItems.RemoveRange(wishlistItemsToDelete);

                // Delete Products
                context.Products.RemoveRange(productsToDelete);
                await context.SaveChangesAsync();
            }

            // 2. Clear old categories to rebuild tree
            var allCategories = await context.Categories.ToListAsync();
            var categoriesWithProducts = await context.Products
                .Select(p => p.CategoryId)
                .Distinct()
                .ToListAsync();
            
            var categoriesToDelete = allCategories.Where(c => !categoriesWithProducts.Contains(c.Id)).ToList();
            if (categoriesToDelete.Any())
            {
                context.Categories.RemoveRange(categoriesToDelete);
                await context.SaveChangesAsync();
            }

            // 3. Create new Hierarchy
            var rootKitchen = Category.Create(
                "أدوات ومعدات المطبخ", "Kitchen Tools & Equipment", "kitchen-tools-equipment",
                null, null, null, "fa-utensils", 1);
            var rootHome = Category.Create(
                "المنزل", "Home", "home",
                null, null, null, "fa-home", 2);
            var rootCleaning = Category.Create(
                "العناية بالمنزل والتنظيف", "Home Care & Cleaning", "home-care-cleaning",
                null, null, null, "fa-broom", 3);

            await context.Categories.AddRangeAsync(rootKitchen, rootHome, rootCleaning);
            await context.SaveChangesAsync();

            // Subcategories for Kitchen
            var kitchenSubs = new List<Category>
            {
                Category.Create("الموازين", "Scales", "scales", null, null, rootKitchen.Id, "fa-weight-scale", 1),
                Category.Create("أوانى الطهى", "Cookware", "cookware", null, null, rootKitchen.Id, "fa-fire-burner", 2),
                Category.Create("أدوات حفظ وتخزين الطعام", "Food Storage", "food-storage", null, null, rootKitchen.Id, "fa-box", 3),
                Category.Create("سكاكين وشوك ومعالق", "Cutlery", "cutlery", null, null, rootKitchen.Id, "fa-utensils", 4),
                Category.Create("أكواب وأدوات الشرب", "Cups & Drinkware", "drinkware", null, null, rootKitchen.Id, "fa-mug-hot", 5),
                Category.Create("إكسسوارات المطبخ", "Kitchen Accessories", "kitchen-accessories", null, null, rootKitchen.Id, "fa-blender", 6),
                Category.Create("أوانى مائدة وتقديم", "Tableware & Serving", "tableware-serving", null, null, rootKitchen.Id, "fa-plate-wheat", 7)
            };

            // Subcategories for Home
            var homeSubs = new List<Category>
            {
                Category.Create("الإضاءة وكشافات الطوارئ", "Lighting", "lighting", null, null, rootHome.Id, "fa-lightbulb", 1),
                Category.Create("تحف وأنتيكات", "Antiques", "antiques", null, null, rootHome.Id, "fa-gem", 2),
                Category.Create("مفروشات", "Furnishings", "furnishings", null, null, rootHome.Id, "fa-bed", 3),
                Category.Create("أدوات منزلية", "Home Tools", "home-tools", null, null, rootHome.Id, "fa-hammer", 4),
                Category.Create("بخور", "Incense", "incense", null, null, rootHome.Id, "fa-wind", 5)
            };

            // Subcategories for Cleaning
            var cleaningSubs = new List<Category>
            {
                Category.Create("معطر للجو", "Air Fresheners", "air-fresheners", null, null, rootCleaning.Id, "fa-spray-can", 1),
                Category.Create("مستلزمات التنظيف", "Cleaning Supplies", "cleaning-supplies", null, null, rootCleaning.Id, "fa-soap", 2),
                Category.Create("منظفات منزلية", "Household Cleaners", "household-cleaners", null, null, rootCleaning.Id, "fa-bottle-droplet", 3),
                Category.Create("العناية بالغسيل", "Laundry Care", "laundry-care", null, null, rootCleaning.Id, "fa-shirt", 4),
                Category.Create("مناديل المنزل", "Home Tissues", "home-tissues", null, null, rootCleaning.Id, "fa-toilet-paper", 5),
                Category.Create("ملمع أحذية", "Shoe Polish", "shoe-polish", null, null, rootCleaning.Id, "fa-shoe-prints", 6),
                Category.Create("مكافحة الحشرات", "Pest Control", "pest-control", null, null, rootCleaning.Id, "fa-bug", 7)
            };

            var allSubs = kitchenSubs.Concat(homeSubs).Concat(cleaningSubs).ToList();
            await context.Categories.AddRangeAsync(allSubs);
            await context.SaveChangesAsync();

            // 4. Keyword Mappings for Kitchen subcategories
            var keywordMapping = new Dictionary<string, int>
            {
                { "ميزان", kitchenSubs.First(s => s.Slug == "scales").Id },
                { "موازين", kitchenSubs.First(s => s.Slug == "scales").Id },

                { "حلة", kitchenSubs.First(s => s.Slug == "cookware").Id },
                { "حلل", kitchenSubs.First(s => s.Slug == "cookware").Id },
                { "طاسة", kitchenSubs.First(s => s.Slug == "cookware").Id },
                { "طاسات", kitchenSubs.First(s => s.Slug == "cookware").Id },
                { "قدر", kitchenSubs.First(s => s.Slug == "cookware").Id },
                { "كسرولة", kitchenSubs.First(s => s.Slug == "cookware").Id },
                { "صينية", kitchenSubs.First(s => s.Slug == "cookware").Id },
                { "فرن", kitchenSubs.First(s => s.Slug == "cookware").Id },
                { "طهي", kitchenSubs.First(s => s.Slug == "cookware").Id },
                { "طبخ", kitchenSubs.First(s => s.Slug == "cookware").Id },
                { "مقلاة", kitchenSubs.First(s => s.Slug == "cookware").Id },
                { "تيفال", kitchenSubs.First(s => s.Slug == "cookware").Id },

                { "حفظ", kitchenSubs.First(s => s.Slug == "food-storage").Id },
                { "تخزين", kitchenSubs.First(s => s.Slug == "food-storage").Id },
                { "برطمان", kitchenSubs.First(s => s.Slug == "food-storage").Id },
                { "علبة", kitchenSubs.First(s => s.Slug == "food-storage").Id },
                { "حافظة", kitchenSubs.First(s => s.Slug == "food-storage").Id },
                { "ثلاجة", kitchenSubs.First(s => s.Slug == "food-storage").Id },
                { "ترمس", kitchenSubs.First(s => s.Slug == "food-storage").Id },

                { "سكين", kitchenSubs.First(s => s.Slug == "cutlery").Id },
                { "سكاكين", kitchenSubs.First(s => s.Slug == "cutlery").Id },
                { "شوك", kitchenSubs.First(s => s.Slug == "cutlery").Id },
                { "معالق", kitchenSubs.First(s => s.Slug == "cutlery").Id },
                { "ملعقة", kitchenSubs.First(s => s.Slug == "cutlery").Id },
                { "شوكة", kitchenSubs.First(s => s.Slug == "cutlery").Id },
                { "طقم توزيع", kitchenSubs.First(s => s.Slug == "cutlery").Id },

                { "كوب", kitchenSubs.First(s => s.Slug == "drinkware").Id },
                { "أكواب", kitchenSubs.First(s => s.Slug == "drinkware").Id },
                { "مج", kitchenSubs.First(s => s.Slug == "drinkware").Id },
                { "ماج", kitchenSubs.First(s => s.Slug == "drinkware").Id },
                { "كاس", kitchenSubs.First(s => s.Slug == "drinkware").Id },
                { "زجاجة", kitchenSubs.First(s => s.Slug == "drinkware").Id },
                { "شرب", kitchenSubs.First(s => s.Slug == "drinkware").Id },

                { "طبق", kitchenSubs.First(s => s.Slug == "tableware-serving").Id },
                { "أطباق", kitchenSubs.First(s => s.Slug == "tableware-serving").Id },
                { "صحن", kitchenSubs.First(s => s.Slug == "tableware-serving").Id },
                { "صحون", kitchenSubs.First(s => s.Slug == "tableware-serving").Id },
                { "مائدة", kitchenSubs.First(s => s.Slug == "tableware-serving").Id },
                { "تقديم", kitchenSubs.First(s => s.Slug == "tableware-serving").Id },
                { "سلطانية", kitchenSubs.First(s => s.Slug == "tableware-serving").Id },
                { "بولة", kitchenSubs.First(s => s.Slug == "tableware-serving").Id },

                { "مبشرة", kitchenSubs.First(s => s.Slug == "kitchen-accessories").Id },
                { "فتاحة", kitchenSubs.First(s => s.Slug == "kitchen-accessories").Id },
                { "هراسة", kitchenSubs.First(s => s.Slug == "kitchen-accessories").Id },
                { "مصفاة", kitchenSubs.First(s => s.Slug == "kitchen-accessories").Id },
                { "قطاعة", kitchenSubs.First(s => s.Slug == "kitchen-accessories").Id },
                { "خلاط", kitchenSubs.First(s => s.Slug == "kitchen-accessories").Id },
                { "عصارة", kitchenSubs.First(s => s.Slug == "kitchen-accessories").Id },
                { "مفرمة", kitchenSubs.First(s => s.Slug == "kitchen-accessories").Id },
                { "مضرب", kitchenSubs.First(s => s.Slug == "kitchen-accessories").Id },
                { "ولاعة", kitchenSubs.First(s => s.Slug == "kitchen-accessories").Id }
            };

            // 5. Seed Products
            var productsToInsert = new List<Product>();
            for (int i = 0; i < payload.Products.Count; i++)
            {
                var seedProd = payload.Products[i];
                
                // Skip if duplicate SKU in current batch (just in case)
                if (productsToInsert.Any(p => p.SKU == seedProd.Sku.ToUpperInvariant()))
                {
                    continue;
                }

                int assignedCategoryId = rootKitchen.Id; // Fallback to root kitchen category
                foreach (var kvp in keywordMapping)
                {
                    if (seedProd.NameAr.Contains(kvp.Key, StringComparison.InvariantCultureIgnoreCase) || 
                        (seedProd.DescriptionAr != null && seedProd.DescriptionAr.Contains(kvp.Key, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        assignedCategoryId = kvp.Value;
                        break;
                    }
                }

                var product = Product.Create(
                    nameAr: seedProd.NameAr,
                    nameEn: seedProd.NameEn,
                    slug: seedProd.Slug,
                    sku: seedProd.Sku,
                    price: seedProd.Price,
                    categoryId: assignedCategoryId,
                    stockQuantity: seedProd.StockQuantity,
                    descriptionAr: seedProd.DescriptionAr,
                    descriptionEn: seedProd.DescriptionEn
                );

                if (seedProd.OriginalPrice.HasValue && seedProd.OriginalPrice.Value > seedProd.Price)
                {
                    product.SetDiscount(seedProd.OriginalPrice.Value);
                }

                product.SetImportMetadata(seedProd.SourceUrl, DateTime.UtcNow);

                if (i < 12)
                {
                    product.SetFeatured(true);
                }

                // Add Images
                int imageOrder = 1;
                foreach (var imageUrl in seedProd.ImageUrls)
                {
                    var image = ProductImage.Create(
                        productId: 0,
                        imageUrl: imageUrl,
                        thumbnailUrl: null,
                        altTextAr: seedProd.NameAr,
                        altTextEn: seedProd.NameEn,
                        displayOrder: imageOrder,
                        isPrimary: imageOrder == 1
                    );
                    product.Images.Add(image);
                    imageOrder++;
                }

                productsToInsert.Add(product);
            }

            if (productsToInsert.Any())
            {
                await context.Products.AddRangeAsync(productsToInsert);
                await context.SaveChangesAsync();
            }
        }

        private class SeedPayload
        {
            public string Source { get; set; } = string.Empty;
            public string ScrapedAtUtc { get; set; } = string.Empty;
            public SeedCategory Category { get; set; } = null!;
            public List<SeedProduct> Products { get; set; } = null!;
        }

        private class SeedCategory
        {
            public string NameAr { get; set; } = string.Empty;
            public string NameEn { get; set; } = string.Empty;
            public string Slug { get; set; } = string.Empty;
            public string? DescriptionAr { get; set; }
            public string? DescriptionEn { get; set; }
            public string? ImageUrl { get; set; }
        }

        private class SeedProduct
        {
            public string SourceId { get; set; } = string.Empty;
            public string Sku { get; set; } = string.Empty;
            public string Slug { get; set; } = string.Empty;
            public string NameAr { get; set; } = string.Empty;
            public string NameEn { get; set; } = string.Empty;
            public string? DescriptionAr { get; set; }
            public string? DescriptionEn { get; set; }
            public decimal Price { get; set; }
            public decimal? OriginalPrice { get; set; }
            public int StockQuantity { get; set; }
            public string? Material { get; set; }
            public string? Origin { get; set; }
            public string SourceUrl { get; set; } = string.Empty;
            public List<string> ImageUrls { get; set; } = null!;
        }
    }
}
