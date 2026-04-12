using GalleryBetak.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GalleryBetak.Infrastructure.Data
{
    public static class AppDbContextSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            List<Category>? seedCategories = null;

            if (!context.Categories.Any())
            {
                seedCategories = new List<Category>
                {
                    Category.Create("أثاث منزلي", "Home Furniture", "home-furniture", null, null, null, "M19 21V5...", 1),
                    Category.Create("ديكورات", "Decorations", "decorations", null, null, null, "M5 3v4...", 2),
                    Category.Create("إضاءة", "Lighting", "lighting", null, null, null, "M9.663 17h4.673...", 3),
                    Category.Create("سجاد", "Rugs", "rugs", null, null, null, "M4 5a1...", 4),
                    Category.Create("أقمشة ومفارش", "Textiles", "textiles", null, null, null, "M3 6h18...", 5)
                };

                await context.Categories.AddRangeAsync(seedCategories);
                await context.SaveChangesAsync();
            }

            if (!context.Products.Any())
            {
                var categories = seedCategories ?? await context.Categories
                    .OrderBy(c => c.DisplayOrder)
                    .Take(5)
                    .ToListAsync();

                if (categories.Count == 0)
                {
                    return;
                }

                int CategoryIdAt(int index)
                {
                    if (index < categories.Count)
                    {
                        return categories[index].Id;
                    }

                    return categories[0].Id;
                }

                var products = new List<Product>
                {
                    Product.Create("طقم جلوس فاخر كلاسيك 8 قطع", "Classic Sofa Set 8 Pieces", "أريكة فاخرة بتصميم كلاسيكي ومقاعد مريحة", "Luxury classic sofa set with premium comfort", 45000, "SKU001", 10, CategoryIdAt(0)),
                    Product.Create("نجفة كريستال بوهيمي ذهبي", "Crystal Chandelier Gold", "نجفة بتصميم أنيق تناسب غرف المعيشة والاستقبال", "Elegant chandelier for living and dining spaces", 12500, "SKU002", 5, CategoryIdAt(2)),
                    Product.Create("سجادة حرير إيراني 2x3", "Iranian Silk Rug 2x3", "سجادة فاخرة بألوان دافئة ونقوش تقليدية", "Premium rug with warm tones and traditional patterns", 10500, "SKU003", 20, CategoryIdAt(3)),
                    Product.Create("طاولة طعام خشب بلوط مع 6 كراسي", "Oak Dining Table 6 Chairs", "طاولة طعام متينة مع ستة كراسي مريحة", "Durable dining table with six chairs", 22000, "SKU004", 2, CategoryIdAt(0)),
                    Product.Create("مزهرية سيراميك مطفية", "Matte Ceramic Vase", "مزهرية ديكورية بتشطيب مطفي", "Decorative ceramic vase with matte finish", 1850, "SKU005", 40, CategoryIdAt(1)),
                    Product.Create("مصباح أرضي معدني أسود", "Black Metal Floor Lamp", "مصباح أرضي عصري بقاعدة مستقرة", "Modern floor lamp with sturdy base", 4200, "SKU006", 18, CategoryIdAt(2)),
                    Product.Create("وسادة مخملية مطرزة", "Embroidered Velvet Cushion", "وسادة فاخرة تضيف لمسة دافئة للغرفة", "Soft velvet cushion with embroidered details", 950, "SKU007", 60, CategoryIdAt(4)),
                    Product.Create("طقم ستائر شيفون مزدوج", "Double Chiffon Curtain Set", "ستائر خفيفة تسمح بمرور الضوء بشكل أنيق", "Lightweight curtains that soften daylight", 3200, "SKU008", 25, CategoryIdAt(4)),
                    Product.Create("رف جداري خشب طبيعي", "Natural Wood Wall Shelf", "رف جداري عملي للديكور والتنظيم", "Practical wall shelf for decor and storage", 2400, "SKU009", 30, CategoryIdAt(0)),
                    Product.Create("لوحة جدارية هندسية", "Geometric Wall Art", "لوحة ديكور بتصميم هندسي معاصر", "Contemporary geometric wall art", 2700, "SKU010", 14, CategoryIdAt(1))
                };
                
                products[2].SetDiscount(12500); // Set discount to generate correct original price logic
                products[0].SetDiscount(50000);
                products[3].SetDiscount(26000);
                products[5].SetDiscount(5000);
                products[7].SetDiscount(4100);
                
                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }

            if (!context.ProductImages.Any())
            {
                var products = await context.Products
                    .OrderBy(p => p.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                if (products.Count == 0)
                {
                    return;
                }

                var productImages = new List<ProductImage>();

                for (var index = 0; index < products.Count; index++)
                {
                    var product = products[index];
                    productImages.Add(
                        ProductImage.Create(
                            product.Id,
                            "/assets/seed-images/geometric-wall-art.svg",
                            null,
                            product.NameAr,
                            product.NameEn,
                            1,
                            true));
                }

                await context.ProductImages.AddRangeAsync(productImages);
                await context.SaveChangesAsync();
            }
        }
    }
}

