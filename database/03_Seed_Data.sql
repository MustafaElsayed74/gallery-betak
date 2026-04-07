-- =====================================================================
-- GalleryBetak E-Commerce — Seed Data
-- Execute AFTER 01_DDL_Schema.sql
-- =====================================================================

SET NOCOUNT ON;

-- =====================================================================
-- SEED: Categories (5 main + subcategories)
-- =====================================================================

SET IDENTITY_INSERT [dbo].[Categories] ON;

-- Root Categories
INSERT INTO [dbo].[Categories] ([Id], [NameAr], [NameEn], [Slug], [DescriptionAr], [DescriptionEn], [ImageUrl], [ParentId], [DisplayOrder], [IsActive])
VALUES
    (1,  N'مستلزمات المنزل',    'Home Accessories',     'home-accessories',
         N'كل ما يلزم منزلك من أدوات ومستلزمات عالية الجودة',
         'Everything your home needs - high quality tools and supplies',
         '/images/categories/home.webp', NULL, 1, 1),
    (2,  N'أدوات المطبخ',       'Kitchen Tools',        'kitchen-tools',
         N'أدوات مطبخ عملية وعصرية لتسهيل حياتك اليومية',
         'Practical and modern kitchen tools for everyday life',
         '/images/categories/kitchen.webp', NULL, 2, 1),
    (3,  N'ديكور',              'Decor',                'decor',
         N'قطع ديكور مميزة تضيف لمسة جمالية لمنزلك',
         'Unique decor pieces that add beauty to your home',
         '/images/categories/decor.webp', NULL, 3, 1),
    (4,  N'تحف وأنتيكات',       'Antiques',             'antiques',
         N'تحف وأنتيكات نادرة ومميزة تعكس التراث والأصالة',
         'Rare and unique antiques reflecting heritage and authenticity',
         '/images/categories/antiques.webp', NULL, 4, 1),
    (5,  N'مفارش وأقمشة',       'Table Covers & Fabrics', 'table-covers-fabrics',
         N'مفارش وأقمشة بتصاميم متنوعة لكل المناسبات',
         'Table covers and fabrics with diverse designs for all occasions',
         '/images/categories/fabrics.webp', NULL, 5, 1);

-- Subcategories
INSERT INTO [dbo].[Categories] ([Id], [NameAr], [NameEn], [Slug], [DescriptionAr], [DescriptionEn], [ImageUrl], [ParentId], [DisplayOrder], [IsActive])
VALUES
    -- Home Accessories subcategories
    (6,  N'إضاءة',             'Lighting',              'lighting',
         N'وحدات إضاءة ديكورية وعملية', 'Decorative and functional lighting',
         '/images/categories/lighting.webp', 1, 1, 1),
    (7,  N'تنظيم وتخزين',      'Storage & Organization', 'storage-organization',
         N'حلول تخزين وتنظيم ذكية', 'Smart storage and organization solutions',
         '/images/categories/storage.webp', 1, 2, 1),

    -- Kitchen Tools subcategories
    (8,  N'أواني طهي',          'Cookware',              'cookware',
         N'أواني طهي بجودة عالية', 'High quality cookware',
         '/images/categories/cookware.webp', 2, 1, 1),
    (9,  N'أدوات تقديم',        'Serving Ware',          'serving-ware',
         N'أدوات تقديم أنيقة', 'Elegant serving ware',
         '/images/categories/serving.webp', 2, 2, 1),

    -- Decor subcategories
    (10, N'لوحات جدارية',       'Wall Art',              'wall-art',
         N'لوحات جدارية فنية', 'Artistic wall paintings',
         '/images/categories/wall-art.webp', 3, 1, 1),
    (11, N'مزهريات',            'Vases',                 'vases',
         N'مزهريات ديكورية بأشكال متنوعة', 'Decorative vases in various shapes',
         '/images/categories/vases.webp', 3, 2, 1);

SET IDENTITY_INSERT [dbo].[Categories] OFF;

-- =====================================================================
-- SEED: Tags
-- =====================================================================

SET IDENTITY_INSERT [dbo].[Tags] ON;

INSERT INTO [dbo].[Tags] ([Id], [NameAr], [NameEn], [Slug])
VALUES
    (1, N'جديد',       'New Arrival',   'new-arrival'),
    (2, N'الأكثر مبيعاً', 'Best Seller', 'best-seller'),
    (3, N'عرض خاص',    'Special Offer', 'special-offer'),
    (4, N'صناعة يدوية', 'Handmade',     'handmade'),
    (5, N'صديق للبيئة', 'Eco-Friendly', 'eco-friendly');

SET IDENTITY_INSERT [dbo].[Tags] OFF;

-- =====================================================================
-- SEED: Products (2 per main category = 10 products)
-- =====================================================================

SET IDENTITY_INSERT [dbo].[Products] ON;

INSERT INTO [dbo].[Products] ([Id], [NameAr], [NameEn], [Slug], [DescriptionAr], [DescriptionEn], [Price], [OriginalPrice], [SKU], [StockQuantity], [CategoryId], [IsFeatured], [IsActive], [Material], [Origin])
VALUES
    -- Home Accessories (Category 1)
    (1, N'مجموعة وسائد مخملية فاخرة',    'Luxury Velvet Pillow Set',     'luxury-velvet-pillow-set',
        N'مجموعة من 4 وسائد مخملية بألوان خريفية دافئة. مناسبة للكنب والأسرة. حشو مريح وقماش مقاوم للبهتان.',
        'Set of 4 velvet pillows in warm autumn colors. Suitable for sofas and beds. Comfortable filling with fade-resistant fabric.',
        450.00, 550.00, 'HOME-PLW-001', 25, 1, 1, 1, N'مخمل مستورد', N'تركيا'),
    (2, N'ساعة حائط خشبية كلاسيكية',      'Classic Wooden Wall Clock',    'classic-wooden-wall-clock',
        N'ساعة حائط خشبية بتصميم كلاسيكي أنيق. قطر 40 سم. حركة صامتة. مناسبة لجميع الغرف.',
        'Wooden wall clock with elegant classic design. 40cm diameter. Silent movement. Suitable for all rooms.',
        320.00, NULL, 'HOME-CLK-002', 15, 1, 1, 1, N'خشب زان', N'مصر'),

    -- Kitchen Tools (Category 2)
    (3, N'طقم أواني جرانيت 7 قطع',        'Granite Cookware Set 7 Pcs',   'granite-cookware-set-7pcs',
        N'طقم أواني طهي جرانيت 7 قطع مقاوم للالتصاق. يشمل حلل وطاسات بأحجام متنوعة. آمن للاستخدام على جميع أنواع البوتاجازات.',
        '7-piece non-stick granite cookware set. Includes pots and pans in various sizes. Safe for all stovetop types.',
        1250.00, 1500.00, 'KIT-GRN-001', 30, 2, 1, 1, N'جرانيت كوري', N'كوريا الجنوبية'),
    (4, N'طقم سكاكين استانلس ستيل 5 قطع',  'Stainless Steel Knife Set 5 Pcs', 'stainless-knife-set-5pcs',
        N'طقم سكاكين احترافية من الاستانلس ستيل. 5 سكاكين بأحجام مختلفة مع حامل خشبي أنيق.',
        'Professional stainless steel knife set. 5 knives in different sizes with an elegant wooden holder.',
        380.00, NULL, 'KIT-KNF-002', 20, 2, 0, 1, N'استانلس ستيل', N'ألمانيا'),

    -- Decor (Category 3)
    (5, N'مزهرية سيراميك مع نقوش ذهبية',   'Ceramic Vase with Gold Engravings', 'ceramic-vase-gold-engravings',
        N'مزهرية سيراميك فاخرة بنقوش ذهبية يدوية الصنع. ارتفاع 35 سم. قطعة ديكورية مميزة لأي غرفة.',
        'Luxury ceramic vase with handmade gold engravings. 35cm height. A distinctive decorative piece for any room.',
        280.00, 350.00, 'DEC-VAS-001', 12, 11, 1, 1, N'سيراميك', N'مصر'),
    (6, N'لوحة جدارية تجريدية 60x90',      'Abstract Wall Painting 60x90',     'abstract-wall-painting-60x90',
        N'لوحة جدارية بطباعة فنية تجريدية على كانفاس. مقاس 60×90 سم. إطار خشبي مخفي.',
        'Abstract art print on canvas. Size 60×90 cm. Hidden wooden frame.',
        520.00, NULL, 'DEC-ART-002', 8, 10, 1, 1, N'كانفاس وخشب', N'مصر'),

    -- Antiques (Category 4)
    (7, N'صندوق خشبي عتيق منحوت يدوياً',   'Handcarved Antique Wooden Box',    'handcarved-antique-wooden-box',
        N'صندوق خشبي عتيق منحوت يدوياً بنقوش شرقية. قطعة فريدة من نوعها. مناسب لتخزين المجوهرات والتحف الصغيرة.',
        'Handcarved antique wooden box with oriental engravings. One of a kind. Suitable for storing jewelry and small collectibles.',
        750.00, NULL, 'ANT-BOX-001', 5, 4, 1, 1, N'خشب جوز', N'سوريا'),
    (8, N'تمثال نحاسي فرعوني صغير',        'Small Pharaonic Brass Statue',      'pharaonic-brass-statue-small',
        N'تمثال نحاسي بتصميم فرعوني أصيل. ارتفاع 25 سم. صناعة خان الخليلي. مناسب كهدية أو قطعة ديكورية.',
        'Brass statue with authentic pharaonic design. 25cm height. Made in Khan el-Khalili. Suitable as a gift or decorative piece.',
        420.00, 500.00, 'ANT-STT-002', 10, 4, 0, 1, N'نحاس', N'مصر — خان الخليلي'),

    -- Table Covers & Fabrics (Category 5)
    (9, N'مفرش طاولة قطني مطرز 180x300',   'Embroidered Cotton Table Cover 180x300', 'embroidered-cotton-table-cover',
        N'مفرش طاولة قطني مطرز بأنماط شرقية تقليدية. مقاس 180×300 سم. مناسب لطاولة 8 أشخاص. قابل للغسيل.',
        'Embroidered cotton table cover with traditional oriental patterns. Size 180×300 cm. Fits 8-person table. Machine washable.',
        350.00, 400.00, 'FAB-TBL-001', 18, 5, 1, 1, N'قطن مصري 100%', N'مصر'),
    (10, N'طقم مفارش ستان فاخر 6 قطع',     'Luxury Satin Placemat Set 6 Pcs',   'luxury-satin-placemat-set-6pcs',
        N'طقم مفارش ستان فاخر 6 قطع مع 6 حلقات فوط. ألوان متنوعة. مناسب للعزائم والمناسبات.',
        '6-piece luxury satin placemat set with 6 napkin rings. Various colors. Perfect for dinner parties and occasions.',
        280.00, NULL, 'FAB-PLC-002', 22, 5, 0, 1, N'ستان', N'الصين');

SET IDENTITY_INSERT [dbo].[Products] OFF;

-- =====================================================================
-- SEED: Product Images
-- =====================================================================

INSERT INTO [dbo].[ProductImages] ([ProductId], [ImageUrl], [ThumbnailUrl], [AltTextAr], [AltTextEn], [DisplayOrder], [IsPrimary])
VALUES
    (1, '/uploads/products/luxury-velvet-pillow-set/main.webp', '/uploads/products/luxury-velvet-pillow-set/thumb_300.webp', N'وسائد مخملية فاخرة', 'Luxury Velvet Pillows', 1, 1),
    (2, '/uploads/products/classic-wooden-wall-clock/main.webp', '/uploads/products/classic-wooden-wall-clock/thumb_300.webp', N'ساعة حائط خشبية', 'Wooden Wall Clock', 1, 1),
    (3, '/uploads/products/granite-cookware-set-7pcs/main.webp', '/uploads/products/granite-cookware-set-7pcs/thumb_300.webp', N'طقم أواني جرانيت', 'Granite Cookware Set', 1, 1),
    (4, '/uploads/products/stainless-knife-set-5pcs/main.webp', '/uploads/products/stainless-knife-set-5pcs/thumb_300.webp', N'طقم سكاكين', 'Knife Set', 1, 1),
    (5, '/uploads/products/ceramic-vase-gold-engravings/main.webp', '/uploads/products/ceramic-vase-gold-engravings/thumb_300.webp', N'مزهرية سيراميك', 'Ceramic Vase', 1, 1),
    (6, '/uploads/products/abstract-wall-painting-60x90/main.webp', '/uploads/products/abstract-wall-painting-60x90/thumb_300.webp', N'لوحة جدارية', 'Wall Painting', 1, 1),
    (7, '/uploads/products/handcarved-antique-wooden-box/main.webp', '/uploads/products/handcarved-antique-wooden-box/thumb_300.webp', N'صندوق خشبي عتيق', 'Antique Box', 1, 1),
    (8, '/uploads/products/pharaonic-brass-statue-small/main.webp', '/uploads/products/pharaonic-brass-statue-small/thumb_300.webp', N'تمثال فرعوني', 'Pharaonic Statue', 1, 1),
    (9, '/uploads/products/embroidered-cotton-table-cover/main.webp', '/uploads/products/embroidered-cotton-table-cover/thumb_300.webp', N'مفرش طاولة مطرز', 'Embroidered Table Cover', 1, 1),
    (10, '/uploads/products/luxury-satin-placemat-set-6pcs/main.webp', '/uploads/products/luxury-satin-placemat-set-6pcs/thumb_300.webp', N'مفارش ستان', 'Satin Placemats', 1, 1);

-- =====================================================================
-- SEED: Product Tags
-- =====================================================================

INSERT INTO [dbo].[ProductTags] ([ProductId], [TagId])
VALUES
    (1, 1), (1, 3),   -- Velvet Pillows: New, Special Offer
    (3, 2), (3, 3),   -- Granite Cookware: Best Seller, Special Offer
    (5, 4),            -- Ceramic Vase: Handmade
    (6, 1),            -- Wall Painting: New
    (7, 4),            -- Antique Box: Handmade
    (8, 4),            -- Pharaonic Statue: Handmade
    (9, 4), (9, 5);   -- Table Cover: Handmade, Eco-Friendly

-- =====================================================================
-- SEED: Coupons (3 sample)
-- =====================================================================

INSERT INTO [dbo].[Coupons] ([Code], [DescriptionAr], [DescriptionEn], [DiscountType], [DiscountValue], [MinOrderAmount], [MaxDiscountAmount], [UsageLimit], [StartsAt], [ExpiresAt], [IsActive])
VALUES
    (N'WELCOME10',
     N'خصم 10% للعملاء الجدد',
     'Welcome 10% discount for new customers',
     N'Percentage', 10.00, 200.00, 100.00, 500,
     '2026-01-01', '2026-12-31', 1),

    (N'RAMADAN50',
     N'خصم 50 جنيه بمناسبة رمضان',
     'Ramadan discount - 50 EGP off',
     N'FixedAmount', 50.00, 300.00, NULL, 200,
     '2026-03-01', '2026-04-15', 1),

    (N'SUMMER25',
     N'خصم 25% على مجموعة الصيف',
     'Summer collection 25% off',
     N'Percentage', 25.00, 500.00, 200.00, 100,
     '2026-06-01', '2026-08-31', 1);

-- =====================================================================
-- SEED: SuperAdmin User
-- Identity tables are seeded via EF Core DatabaseInitializer.
-- Below is the reference for the manual seed if needed:
-- =====================================================================

-- NOTE: SuperAdmin user is created through ASP.NET Identity in code:
--   Email: admin@gallery-betak.com
--   Password: Admin@123456 (changed on first login)
--   Role: SuperAdmin
--   FirstName: مدير النظام
--   LastName: الرئيسي
-- This is handled by DatabaseInitializer.SeedAsync() in Infrastructure layer.

GO
PRINT N'Seed data inserted successfully.';
GO

