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
            if (await context.Categories.AnyAsync(c => c.NameAr == "حقائب وإكسسوارات" && c.ParentId == null))
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

            // 1. Delete all existing records to re-seed cleanly and avoid FK constraint conflicts
            context.OrderItems.RemoveRange(await context.OrderItems.ToListAsync());
            context.Payments.RemoveRange(await context.Payments.ToListAsync());
            context.Orders.RemoveRange(await context.Orders.ToListAsync());
            context.CartItems.RemoveRange(await context.CartItems.ToListAsync());
            context.WishlistItems.RemoveRange(await context.WishlistItems.ToListAsync());
            context.ReviewImages.RemoveRange(await context.ReviewImages.ToListAsync());
            context.Reviews.RemoveRange(await context.Reviews.ToListAsync());
            context.ProductTags.RemoveRange(await context.ProductTags.ToListAsync());
            context.ProductImages.RemoveRange(await context.ProductImages.ToListAsync());
            context.Products.RemoveRange(await context.Products.ToListAsync());
            context.Categories.RemoveRange(await context.Categories.ToListAsync());
            await context.SaveChangesAsync();

            // 3. Create new Hierarchy for Merakii Bags & Accessories
            var rootBags = Category.Create(
                "حقائب وإكسسوارات", "Bags & Accessories", "bags-accessories",
                null, null, null, "fa-bag-shopping", 1);

            await context.Categories.AddAsync(rootBags);
            await context.SaveChangesAsync();

            // Subcategories for Bags
            var bagSubs = new List<Category>
            {
                Category.Create("حقائب يد", "Handbags", "handbags", null, null, rootBags.Id, "fa-bag-shopping", 1),
                Category.Create("حقائب كتف", "Shoulder Bags", "shoulder-bags", null, null, rootBags.Id, "fa-bag-shopping", 2),
                Category.Create("حقائب ظهر", "Backpacks", "backpacks", null, null, rootBags.Id, "fa-backpack", 3),
                Category.Create("حقائب مكياج", "Cosmetic Bags", "cosmetic-bags", null, null, rootBags.Id, "fa-circle-dot", 4),
                Category.Create("إكسسوارات لطيفة", "Cute Accessories", "cute-accessories", null, null, rootBags.Id, "fa-gem", 5)
            };

            await context.Categories.AddRangeAsync(bagSubs);
            await context.SaveChangesAsync();

            // 4. Keyword Mappings for Bags subcategories
            var keywordMapping = new Dictionary<string, int>
            {
                { "يد", bagSubs.First(s => s.Slug == "handbags").Id },
                { "توت", bagSubs.First(s => s.Slug == "handbags").Id },
                { "كتف", bagSubs.First(s => s.Slug == "shoulder-bags").Id },
                { "كروس", bagSubs.First(s => s.Slug == "shoulder-bags").Id },
                { "ظهر", bagSubs.First(s => s.Slug == "backpacks").Id },
                { "مكياج", bagSubs.First(s => s.Slug == "cosmetic-bags").Id },
                { "تجميل", bagSubs.First(s => s.Slug == "cosmetic-bags").Id },
                { "دلاية", bagSubs.First(s => s.Slug == "cute-accessories").Id },
                { "ربطة", bagSubs.First(s => s.Slug == "cute-accessories").Id }
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

                int assignedCategoryId = bagSubs.First(s => s.Slug == "handbags").Id; // Fallback to Handbags
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
