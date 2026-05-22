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
            // If kitchen tools are already seeded, skip to avoid overhead on every startup
            if (await context.Products.AnyAsync(p => p.SKU.StartsWith("ELG-")))
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

            // 2. Delete Categories that are the legacy ones and have no products left
            var legacyCategorySlugs = new[] { "home-furniture", "decorations", "lighting", "rugs", "textiles" };
            var categoriesToDelete = await context.Categories
                .Where(c => legacyCategorySlugs.Contains(c.Slug) && !context.Products.Any(p => p.CategoryId == c.Id))
                .ToListAsync();

            if (categoriesToDelete.Any())
            {
                context.Categories.RemoveRange(categoriesToDelete);
                await context.SaveChangesAsync();
            }

            // 3. Find or Create Kitchen Tools category
            var kitchenCategory = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "kitchen-tools");
            if (kitchenCategory == null)
            {
                kitchenCategory = Category.Create(
                    payload.Category.NameAr,
                    payload.Category.NameEn,
                    payload.Category.Slug,
                    payload.Category.DescriptionAr,
                    payload.Category.DescriptionEn,
                    null,
                    payload.Category.ImageUrl,
                    1
                );
                await context.Categories.AddAsync(kitchenCategory);
                await context.SaveChangesAsync();
            }

            // 4. Seed Products
            var productsToInsert = new List<Product>();
            for (int i = 0; i < payload.Products.Count; i++)
            {
                var seedProd = payload.Products[i];
                
                // Skip if duplicate SKU in current batch (just in case)
                if (productsToInsert.Any(p => p.SKU == seedProd.Sku.ToUpperInvariant()))
                {
                    continue;
                }

                var product = Product.Create(
                    nameAr: seedProd.NameAr,
                    nameEn: seedProd.NameEn,
                    slug: seedProd.Slug,
                    sku: seedProd.Sku,
                    price: seedProd.Price,
                    categoryId: kitchenCategory.Id,
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
