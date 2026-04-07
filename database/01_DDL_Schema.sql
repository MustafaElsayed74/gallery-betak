-- =====================================================================
-- GalleryBetak E-Commerce Platform — Complete DDL Schema
-- Database: SQL Server 2022+
-- Encoding: UTF-8 (NVARCHAR for all Arabic-capable fields)
-- Created: 2026-04-01
-- =====================================================================

-- =====================================================================
-- SECTION 1: IDENTITY TABLES (ASP.NET Identity Extended)
-- =====================================================================

-- AspNetRoles is created by Identity framework, we extend via ApplicationRole
-- AspNetUsers is created by Identity framework, we extend via ApplicationUser

-- Extended User Profile Fields (added to AspNetUsers via Identity)
-- These columns are added through ApplicationUser entity in EF Core:
--   FirstName       NVARCHAR(100)   NOT NULL
--   LastName        NVARCHAR(100)   NOT NULL
--   ProfileImageUrl NVARCHAR(500)   NULL
--   RefreshToken    NVARCHAR(500)   NULL  (stored hashed — SHA-256)
--   RefreshTokenExpiryTime DATETIME2 NULL
--   IsActive        BIT             NOT NULL DEFAULT 1
--   LastLoginAt     DATETIME2       NULL
--   CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
--   UpdatedAt       DATETIME2       NULL
--   IsDeleted       BIT             NOT NULL DEFAULT 0
--   DeletedAt       DATETIME2       NULL

-- =====================================================================
-- SECTION 2: CATEGORIES (self-referencing hierarchy)
-- =====================================================================

CREATE TABLE [dbo].[Categories]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [NameAr] NVARCHAR(300) NOT NULL,
    [NameEn] NVARCHAR(300) NOT NULL,
    [Slug] NVARCHAR(300) NOT NULL,
    [DescriptionAr] NVARCHAR(MAX) NULL,
    [DescriptionEn] NVARCHAR(MAX) NULL,
    [ImageUrl] NVARCHAR(500) NULL,
    [ParentId] INT NULL,
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    [CreatedBy] NVARCHAR(450) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [DeletedAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Categories_ParentCategory] FOREIGN KEY ([ParentId])
        REFERENCES [dbo].[Categories]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [UQ_Categories_Slug] UNIQUE ([Slug])
);

CREATE NONCLUSTERED INDEX [IX_Categories_ParentId] ON [dbo].[Categories] ([ParentId])
    WHERE [IsDeleted] = 0;
CREATE NONCLUSTERED INDEX [IX_Categories_Slug] ON [dbo].[Categories] ([Slug])
    WHERE [IsDeleted] = 0;
CREATE NONCLUSTERED INDEX [IX_Categories_IsActive_DisplayOrder] ON [dbo].[Categories] ([IsActive], [DisplayOrder])
    INCLUDE ([NameAr], [NameEn], [Slug], [ImageUrl], [ParentId])
    WHERE [IsDeleted] = 0;

-- =====================================================================
-- SECTION 3: TAGS
-- =====================================================================

CREATE TABLE [dbo].[Tags]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [NameAr] NVARCHAR(100) NOT NULL,
    [NameEn] NVARCHAR(100) NOT NULL,
    [Slug] NVARCHAR(100) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [DeletedAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_Tags] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Tags_Slug] UNIQUE ([Slug])
);

-- =====================================================================
-- SECTION 4: PRODUCTS
-- =====================================================================

CREATE TABLE [dbo].[Products]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [NameAr] NVARCHAR(300) NOT NULL,
    [NameEn] NVARCHAR(300) NOT NULL,
    [Slug] NVARCHAR(300) NOT NULL,
    [DescriptionAr] NVARCHAR(MAX) NULL,
    [DescriptionEn] NVARCHAR(MAX) NULL,
    [Price] DECIMAL(18,2) NOT NULL,
    [OriginalPrice] DECIMAL(18,2) NULL,
    [SKU] NVARCHAR(50) NOT NULL,
    [StockQuantity] INT NOT NULL DEFAULT 0,
    [CategoryId] INT NOT NULL,
    [IsFeatured] BIT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [Weight] DECIMAL(10,3) NULL,
    [Dimensions] NVARCHAR(100) NULL,
    [Material] NVARCHAR(200) NULL,
    [Origin] NVARCHAR(200) NULL,
    [SourceUrl] NVARCHAR(1000) NULL,
    [ImportedAt] DATETIME2(7) NULL,
    [AverageRating] DECIMAL(3,2) NOT NULL DEFAULT 0.00,
    [ReviewCount] INT NOT NULL DEFAULT 0,
    [ViewCount] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    [CreatedBy] NVARCHAR(450) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [DeletedAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Products_Category] FOREIGN KEY ([CategoryId])
        REFERENCES [dbo].[Categories]([Id]) ON DELETE RESTRICT,
    CONSTRAINT [UQ_Products_SKU] UNIQUE
([SKU]),
    CONSTRAINT [UQ_Products_Slug] UNIQUE
([Slug]),
    CONSTRAINT [CK_Products_Price] CHECK
([Price] > 0),
    CONSTRAINT [CK_Products_OriginalPrice] CHECK
([OriginalPrice] IS NULL OR [OriginalPrice] > [Price]),
    CONSTRAINT [CK_Products_StockQuantity] CHECK
([StockQuantity] >= 0),
    CONSTRAINT [CK_Products_AverageRating] CHECK
([AverageRating] >= 0 AND [AverageRating] <= 5)
);

-- Primary query: product list with filters
CREATE NONCLUSTERED INDEX [IX_Products_CategoryId] ON [dbo].[Products] ([CategoryId])
    INCLUDE ([NameAr], [NameEn], [Price], [IsFeatured], [IsActive])
    WHERE [IsDeleted] = 0;

-- Featured products query
CREATE NONCLUSTERED INDEX [IX_Products_IsFeatured] ON [dbo].[Products] ([IsFeatured], [IsActive])
    INCLUDE ([NameAr], [NameEn], [Price], [OriginalPrice], [Slug])
    WHERE [IsDeleted] = 0 AND [IsFeatured] = 1;

-- Price range filter
CREATE NONCLUSTERED INDEX [IX_Products_Price] ON [dbo].[Products] ([Price])
    INCLUDE ([NameAr], [NameEn], [CategoryId], [IsActive])
    WHERE [IsDeleted] = 0;

-- Slug lookup (SEO URLs)
CREATE NONCLUSTERED INDEX [IX_Products_Slug] ON [dbo].[Products] ([Slug])
    WHERE [IsDeleted] = 0;

-- SKU lookup
CREATE NONCLUSTERED INDEX [IX_Products_SKU] ON [dbo].[Products] ([SKU])
    WHERE [IsDeleted] = 0;

-- New arrivals (sorted by creation date)
CREATE NONCLUSTERED INDEX [IX_Products_CreatedAt] ON [dbo].[Products] ([CreatedAt] DESC)
    INCLUDE ([NameAr], [NameEn], [Price], [Slug])
    WHERE [IsDeleted] = 0 AND [IsActive] = 1;

-- Rating filter
CREATE NONCLUSTERED INDEX [IX_Products_AverageRating] ON [dbo].[Products] ([AverageRating] DESC)
    WHERE [IsDeleted] = 0 AND [IsActive] = 1;

-- Imported products lookup (admin)
CREATE NONCLUSTERED INDEX [IX_Products_ImportedAt] ON [dbo].[Products] ([ImportedAt] DESC)
    WHERE [IsDeleted] = 0 AND [ImportedAt] IS NOT NULL;

-- Low stock alert (admin)
CREATE NONCLUSTERED INDEX [IX_Products_LowStock] ON [dbo].[Products] ([StockQuantity])
    INCLUDE ([NameAr], [SKU])
    WHERE [IsDeleted] = 0 AND [StockQuantity] < 5;

-- Full-Text Search on Arabic + English names and descriptions
CREATE FULLTEXT CATALOG [ProductCatalog] AS DEFAULT;
CREATE FULLTEXT INDEX ON [dbo].[Products] (
    [NameAr] LANGUAGE 0x0401,      -- Arabic
    [NameEn] LANGUAGE 0x0409,      -- English
    [DescriptionAr] LANGUAGE 0x0401,
    [DescriptionEn] LANGUAGE 0x0409
) KEY INDEX [PK_Products] ON [ProductCatalog];

-- =====================================================================
-- SECTION 5: PRODUCT IMAGES
-- =====================================================================

CREATE TABLE [dbo].[ProductImages]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ProductId] INT NOT NULL,
    [ImageUrl] NVARCHAR(500) NOT NULL,
    [ThumbnailUrl] NVARCHAR(500) NULL,
    [AltTextAr] NVARCHAR(300) NULL,
    [AltTextEn] NVARCHAR(300) NULL,
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [IsPrimary] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_ProductImages] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_ProductImages_Product] FOREIGN KEY ([ProductId])
        REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE
);

CREATE NONCLUSTERED INDEX [IX_ProductImages_ProductId] ON [dbo].[ProductImages] ([ProductId], [DisplayOrder]);

-- =====================================================================
-- SECTION 6: PRODUCT TAGS (M:N junction)
-- =====================================================================

CREATE TABLE [dbo].[ProductTags]
(
    [ProductId] INT NOT NULL,
    [TagId] INT NOT NULL,

    CONSTRAINT [PK_ProductTags] PRIMARY KEY CLUSTERED ([ProductId], [TagId]),
    CONSTRAINT [FK_ProductTags_Product] FOREIGN KEY ([ProductId])
        REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductTags_Tag] FOREIGN KEY ([TagId])
        REFERENCES [dbo].[Tags]([Id]) ON DELETE CASCADE
);

CREATE NONCLUSTERED INDEX [IX_ProductTags_TagId] ON [dbo].[ProductTags] ([TagId]);

-- =====================================================================
-- SECTION 7: ADDRESSES
-- =====================================================================

CREATE TABLE [dbo].[Addresses]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    [Label] NVARCHAR(100) NOT NULL,
    -- e.g. "المنزل", "العمل"
    [RecipientName] NVARCHAR(200) NOT NULL,
    [Phone] NVARCHAR(20) NOT NULL,
    [Governorate] NVARCHAR(100) NOT NULL,
    -- محافظة
    [City] NVARCHAR(100) NOT NULL,
    [District] NVARCHAR(100) NULL,
    -- الحي
    [StreetAddress] NVARCHAR(300) NOT NULL,
    [BuildingNo] NVARCHAR(50) NULL,
    [ApartmentNo] NVARCHAR(50) NULL,
    [PostalCode] NVARCHAR(10) NULL,
    [IsDefault] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [DeletedAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_Addresses] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Addresses_User] FOREIGN KEY ([UserId])
        REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_Addresses_Phone] CHECK ([Phone] LIKE '01[0125][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]')
);

CREATE NONCLUSTERED INDEX [IX_Addresses_UserId] ON [dbo].[Addresses] ([UserId])
    WHERE [IsDeleted] = 0;

-- =====================================================================
-- SECTION 8: COUPONS
-- =====================================================================

CREATE TABLE [dbo].[Coupons]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(50) NOT NULL,
    [DescriptionAr] NVARCHAR(300) NULL,
    [DescriptionEn] NVARCHAR(300) NULL,
    [DiscountType] NVARCHAR(20) NOT NULL,
    -- 'Percentage' or 'FixedAmount'
    [DiscountValue] DECIMAL(18,2) NOT NULL,
    [MinOrderAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [MaxDiscountAmount] DECIMAL(18,2) NULL,
    -- cap for percentage discounts
    [UsageLimit] INT NOT NULL DEFAULT 0,
    -- 0 = unlimited
    [UsedCount] INT NOT NULL DEFAULT 0,
    [StartsAt] DATETIME2(7) NOT NULL,
    [ExpiresAt] DATETIME2(7) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    [CreatedBy] NVARCHAR(450) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [DeletedAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_Coupons] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Coupons_Code] UNIQUE ([Code]),
    CONSTRAINT [CK_Coupons_DiscountType] CHECK ([DiscountType] IN (N'Percentage', N'FixedAmount')),
    CONSTRAINT [CK_Coupons_DiscountValue] CHECK ([DiscountValue] > 0),
    CONSTRAINT [CK_Coupons_PercentageRange] CHECK (
        [DiscountType] <> N'Percentage' OR ([DiscountValue] > 0 AND [DiscountValue] <= 100)
    ),
    CONSTRAINT [CK_Coupons_Dates] CHECK ([ExpiresAt] > [StartsAt]),
    CONSTRAINT [CK_Coupons_UsedCount] CHECK ([UsedCount] >= 0)
);

CREATE NONCLUSTERED INDEX [IX_Coupons_Code] ON [dbo].[Coupons] ([Code])
    WHERE [IsDeleted] = 0;
CREATE NONCLUSTERED INDEX [IX_Coupons_Active] ON [dbo].[Coupons] ([IsActive], [StartsAt], [ExpiresAt])
    WHERE [IsDeleted] = 0;

-- =====================================================================
-- SECTION 9: COUPON USAGE TRACKING
-- =====================================================================

CREATE TABLE [dbo].[CouponUsages]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [CouponId] INT NOT NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    [OrderId] INT NOT NULL,
    [DiscountAmount] DECIMAL(18,2) NOT NULL,
    [UsedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_CouponUsages] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_CouponUsages_Coupon] FOREIGN KEY ([CouponId])
        REFERENCES [dbo].[Coupons]([Id]) ON DELETE RESTRICT,
    CONSTRAINT [FK_CouponUsages_User] FOREIGN KEY
([UserId])
        REFERENCES [dbo].[AspNetUsers]
([Id]) ON
DELETE RESTRICT
    -- FK to Orders added after Orders table creation
);

CREATE NONCLUSTERED INDEX [IX_CouponUsages_CouponId] ON [dbo].[CouponUsages] ([CouponId]);
CREATE NONCLUSTERED INDEX [IX_CouponUsages_UserId] ON [dbo].[CouponUsages] ([UserId]);
CREATE UNIQUE INDEX [UQ_CouponUsages_UserCoupon] ON [dbo].[CouponUsages] ([CouponId], [UserId]);

-- =====================================================================
-- SECTION 10: CARTS (supports guest + authenticated)
-- =====================================================================

CREATE TABLE [dbo].[Carts]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] NVARCHAR(450) NULL,
    -- NULL for guest carts
    [SessionId] NVARCHAR(100) NULL,
    -- for guest identification
    [CouponId] INT NULL,
    [ExpiresAt] DATETIME2(7) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_Carts] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Carts_User] FOREIGN KEY ([UserId])
        REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Carts_Coupon] FOREIGN KEY ([CouponId])
        REFERENCES [dbo].[Coupons]([Id]) ON DELETE SET NULL,
    CONSTRAINT [CK_Carts_HasIdentifier] CHECK ([UserId] IS NOT NULL OR [SessionId] IS NOT NULL)
);

CREATE UNIQUE INDEX [UQ_Carts_UserId] ON [dbo].[Carts] ([UserId])
    WHERE [UserId] IS NOT NULL;
CREATE UNIQUE INDEX [UQ_Carts_SessionId] ON [dbo].[Carts] ([SessionId])
    WHERE [SessionId] IS NOT NULL;

-- =====================================================================
-- SECTION 11: CART ITEMS
-- =====================================================================

CREATE TABLE [dbo].[CartItems]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [CartId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [Quantity] INT NOT NULL DEFAULT 1,
    [UnitPrice] DECIMAL(18,2) NOT NULL,           -- snapshot at add time
    [AddedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_CartItems] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_CartItems_Cart] FOREIGN KEY ([CartId])
        REFERENCES [dbo].[Carts]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CartItems_Product] FOREIGN KEY ([ProductId])
        REFERENCES [dbo].[Products]([Id]) ON DELETE RESTRICT,
    CONSTRAINT [CK_CartItems_Quantity] CHECK
([Quantity] > 0 AND [Quantity] <= 99),
    CONSTRAINT [UQ_CartItems_CartProduct] UNIQUE
([CartId], [ProductId])
);

CREATE NONCLUSTERED INDEX [IX_CartItems_CartId] ON [dbo].[CartItems] ([CartId]);
CREATE NONCLUSTERED INDEX [IX_CartItems_ProductId] ON [dbo].[CartItems] ([ProductId]);

-- =====================================================================
-- SECTION 12: WISHLISTS
-- =====================================================================

CREATE TABLE [dbo].[Wishlists]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_Wishlists] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Wishlists_User] FOREIGN KEY ([UserId])
        REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Wishlists_UserId] UNIQUE ([UserId])
);

-- =====================================================================
-- SECTION 13: WISHLIST ITEMS
-- =====================================================================

CREATE TABLE [dbo].[WishlistItems]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [WishlistId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [AddedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_WishlistItems] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_WishlistItems_Wishlist] FOREIGN KEY ([WishlistId])
        REFERENCES [dbo].[Wishlists]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WishlistItems_Product] FOREIGN KEY ([ProductId])
        REFERENCES [dbo].[Products]([Id]) ON DELETE RESTRICT,
    CONSTRAINT [UQ_WishlistItems_WishlistProduct] UNIQUE
([WishlistId], [ProductId])
);

CREATE NONCLUSTERED INDEX [IX_WishlistItems_WishlistId] ON [dbo].[WishlistItems] ([WishlistId]);
CREATE NONCLUSTERED INDEX [IX_WishlistItems_ProductId] ON [dbo].[WishlistItems] ([ProductId]);

-- =====================================================================
-- SECTION 14: ORDERS
-- =====================================================================

CREATE TABLE [dbo].[Orders]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [OrderNumber] NVARCHAR(20) NOT NULL,
    -- Format: ORD-YYYYMM-NNNNN
    [UserId] NVARCHAR(450) NULL,
    -- SET NULL on user delete
    [Status] NVARCHAR(20) NOT NULL DEFAULT N'Pending',
    [SubTotal] DECIMAL(18,2) NOT NULL,
    [ShippingCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [TaxAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [DiscountAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [TotalAmount] DECIMAL(18,2) NOT NULL,
    [CouponId] INT NULL,
    [CouponCode] NVARCHAR(50) NULL,
    -- snapshot
    [PaymentMethod] NVARCHAR(30) NOT NULL,
    [PaymentStatus] NVARCHAR(20) NOT NULL DEFAULT N'Pending',
    [Notes] NVARCHAR(500) NULL,

    -- Address snapshot (frozen at order time)
    [ShippingRecipientName] NVARCHAR(200) NOT NULL,
    [ShippingPhone] NVARCHAR(20) NOT NULL,
    [ShippingGovernorate] NVARCHAR(100) NOT NULL,
    [ShippingCity] NVARCHAR(100) NOT NULL,
    [ShippingDistrict] NVARCHAR(100) NULL,
    [ShippingStreetAddress] NVARCHAR(300) NOT NULL,
    [ShippingBuildingNo] NVARCHAR(50) NULL,
    [ShippingApartmentNo] NVARCHAR(50) NULL,
    [ShippingPostalCode] NVARCHAR(10) NULL,

    [TrackingNumber] NVARCHAR(100) NULL,
    [ShippedAt] DATETIME2(7) NULL,
    [DeliveredAt] DATETIME2(7) NULL,
    [CancelledAt] DATETIME2(7) NULL,
    [CancellationReason] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    [CreatedBy] NVARCHAR(450) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [DeletedAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Orders_User] FOREIGN KEY ([UserId])
        REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Orders_Coupon] FOREIGN KEY ([CouponId])
        REFERENCES [dbo].[Coupons]([Id]) ON DELETE SET NULL,
    CONSTRAINT [UQ_Orders_OrderNumber] UNIQUE ([OrderNumber]),
    CONSTRAINT [CK_Orders_Status] CHECK ([Status] IN (
        N'Pending', N'Confirmed', N'Processing', N'Shipped',
        N'Delivered', N'Cancelled', N'Refunded'
    )),
    CONSTRAINT [CK_Orders_PaymentMethod] CHECK ([PaymentMethod] IN (
        N'VodafoneCash', N'OrangeCash', N'EtisalatCash',
        N'Fawry', N'COD', N'Card'
    )),
    CONSTRAINT [CK_Orders_PaymentStatus] CHECK ([PaymentStatus] IN (
        N'Pending', N'Processing', N'Success', N'Failed', N'Refunded'
    )),
    CONSTRAINT [CK_Orders_TotalAmount] CHECK ([TotalAmount] >= 0),
    CONSTRAINT [CK_Orders_SubTotal] CHECK ([SubTotal] >= 0)
);

-- User orders query
CREATE NONCLUSTERED INDEX [IX_Orders_UserId] ON [dbo].[Orders] ([UserId], [CreatedAt] DESC)
    WHERE [IsDeleted] = 0;

-- Order by number lookup
CREATE NONCLUSTERED INDEX [IX_Orders_OrderNumber] ON [dbo].[Orders] ([OrderNumber])
    WHERE [IsDeleted] = 0;

-- Admin: orders by status
CREATE NONCLUSTERED INDEX [IX_Orders_Status] ON [dbo].[Orders] ([Status], [CreatedAt] DESC)
    INCLUDE ([OrderNumber], [UserId], [TotalAmount], [PaymentStatus])
    WHERE [IsDeleted] = 0;

-- Revenue reporting
CREATE NONCLUSTERED INDEX [IX_Orders_Revenue] ON [dbo].[Orders] ([CreatedAt], [Status])
    INCLUDE ([TotalAmount], [SubTotal], [DiscountAmount])
    WHERE [IsDeleted] = 0 AND [Status] NOT IN (N'Cancelled', N'Refunded');

-- Payment status tracking
CREATE NONCLUSTERED INDEX [IX_Orders_PaymentStatus] ON [dbo].[Orders] ([PaymentStatus])
    WHERE [IsDeleted] = 0;

-- =====================================================================
-- SECTION 15: ORDER ITEMS (product snapshot at order time)
-- =====================================================================

CREATE TABLE [dbo].[OrderItems]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [OrderId] INT NOT NULL,
    [ProductId] INT NULL,
    -- keep reference but don't depend on it
    [ProductNameAr] NVARCHAR(300) NOT NULL,
    -- snapshot
    [ProductNameEn] NVARCHAR(300) NOT NULL,
    -- snapshot
    [ProductSKU] NVARCHAR(50) NOT NULL,
    -- snapshot
    [ProductImageUrl] NVARCHAR(500) NULL,
    -- snapshot
    [UnitPrice] DECIMAL(18,2) NOT NULL,
    -- frozen at order time
    [Quantity] INT NOT NULL,
    [TotalPrice] DECIMAL(18,2) NOT NULL,
    -- UnitPrice * Quantity

    CONSTRAINT [PK_OrderItems] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_OrderItems_Order] FOREIGN KEY ([OrderId])
        REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_Product] FOREIGN KEY ([ProductId])
        REFERENCES [dbo].[Products]([Id]) ON DELETE SET NULL,
    CONSTRAINT [CK_OrderItems_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [CK_OrderItems_UnitPrice] CHECK ([UnitPrice] > 0),
    CONSTRAINT [CK_OrderItems_TotalPrice] CHECK ([TotalPrice] > 0)
);

CREATE NONCLUSTERED INDEX [IX_OrderItems_OrderId] ON [dbo].[OrderItems] ([OrderId]);
CREATE NONCLUSTERED INDEX [IX_OrderItems_ProductId] ON [dbo].[OrderItems] ([ProductId]);

-- Best sellers query: aggregate by ProductId
CREATE NONCLUSTERED INDEX [IX_OrderItems_BestSellers] ON [dbo].[OrderItems] ([ProductId])
    INCLUDE ([Quantity], [TotalPrice]);

-- =====================================================================
-- SECTION 16: PAYMENTS
-- =====================================================================

CREATE TABLE [dbo].[Payments]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [OrderId] INT NOT NULL,
    [TransactionId] NVARCHAR(200) NULL,             -- Paymob transaction ID
    [GatewayOrderId] NVARCHAR(200) NULL,             -- Paymob order ID
    [Amount] DECIMAL(18,2) NOT NULL,
    [Currency] NVARCHAR(3) NOT NULL DEFAULT N'EGP',
    [Method] NVARCHAR(30) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT N'Pending',
    [GatewayResponse] NVARCHAR(MAX) NULL,             -- full JSON response for audit
    [PaidAt] DATETIME2(7) NULL,
    [FailureReason] NVARCHAR(500) NULL,
    [RefundedAt] DATETIME2(7) NULL,
    [RefundAmount] DECIMAL(18,2) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_Payments] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Payments_Order] FOREIGN KEY ([OrderId])
        REFERENCES [dbo].[Orders]([Id]) ON DELETE RESTRICT,
    CONSTRAINT [CK_Payments_Method] CHECK
([Method] IN
(
        N'VodafoneCash', N'OrangeCash', N'EtisalatCash',
        N'Fawry', N'COD', N'Card'
    )),
    CONSTRAINT [CK_Payments_Status] CHECK
([Status] IN
(
        N'Pending', N'Processing', N'Success', N'Failed', N'Refunded'
    )),
    CONSTRAINT [CK_Payments_Amount] CHECK
([Amount] > 0)
);

CREATE NONCLUSTERED INDEX [IX_Payments_OrderId] ON [dbo].[Payments] ([OrderId]);
CREATE UNIQUE INDEX [UQ_Payments_TransactionId] ON [dbo].[Payments] ([TransactionId])
    WHERE [TransactionId] IS NOT NULL;
CREATE NONCLUSTERED INDEX [IX_Payments_GatewayOrderId] ON [dbo].[Payments] ([GatewayOrderId])
    WHERE [GatewayOrderId] IS NOT NULL;
CREATE NONCLUSTERED INDEX [IX_Payments_Status] ON [dbo].[Payments] ([Status]);

-- =====================================================================
-- SECTION 17: REVIEWS
-- =====================================================================

CREATE TABLE [dbo].[Reviews]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ProductId] INT NOT NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    [Rating] INT NOT NULL,
    [Comment] NVARCHAR(2000) NULL,
    [IsVerifiedPurchase] BIT NOT NULL DEFAULT 0,
    [Status] NVARCHAR(20) NOT NULL DEFAULT N'Pending',
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [DeletedAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_Reviews] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Reviews_Product] FOREIGN KEY ([ProductId])
        REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Reviews_User] FOREIGN KEY ([UserId])
        REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_Reviews_Rating] CHECK ([Rating] >= 1 AND [Rating] <= 5),
    CONSTRAINT [CK_Reviews_Status] CHECK ([Status] IN (N'Pending', N'Approved', N'Rejected')),
    CONSTRAINT [UQ_Reviews_UserProduct] UNIQUE ([UserId], [ProductId])
);

CREATE NONCLUSTERED INDEX [IX_Reviews_ProductId] ON [dbo].[Reviews] ([ProductId], [Status])
    WHERE [IsDeleted] = 0;
CREATE NONCLUSTERED INDEX [IX_Reviews_UserId] ON [dbo].[Reviews] ([UserId])
    WHERE [IsDeleted] = 0;
CREATE NONCLUSTERED INDEX [IX_Reviews_Pending] ON [dbo].[Reviews] ([Status])
    INCLUDE ([ProductId], [UserId], [Rating])
    WHERE [IsDeleted] = 0 AND [Status] = N'Pending';

-- =====================================================================
-- SECTION 18: REVIEW IMAGES
-- =====================================================================

CREATE TABLE [dbo].[ReviewImages]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ReviewId] INT NOT NULL,
    [ImageUrl] NVARCHAR(500) NOT NULL,
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_ReviewImages] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_ReviewImages_Review] FOREIGN KEY ([ReviewId])
        REFERENCES [dbo].[Reviews]([Id]) ON DELETE CASCADE
);

CREATE NONCLUSTERED INDEX [IX_ReviewImages_ReviewId] ON [dbo].[ReviewImages] ([ReviewId]);

-- =====================================================================
-- SECTION 19: NOTIFICATIONS
-- =====================================================================

CREATE TABLE [dbo].[Notifications]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    [TitleAr] NVARCHAR(300) NOT NULL,
    [TitleEn] NVARCHAR(300) NOT NULL,
    [MessageAr] NVARCHAR(1000) NOT NULL,
    [MessageEn] NVARCHAR(1000) NOT NULL,
    [Type] NVARCHAR(50) NOT NULL,
    -- OrderUpdate, Payment, Promotion, System
    [ReferenceId] INT NULL,
    -- e.g. OrderId
    [ReferenceType] NVARCHAR(50) NULL,
    -- e.g. "Order", "Product"
    [IsRead] BIT NOT NULL DEFAULT 0,
    [ReadAt] DATETIME2(7) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Notifications_User] FOREIGN KEY ([UserId])
        REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
);

CREATE NONCLUSTERED INDEX [IX_Notifications_UserId] ON [dbo].[Notifications] ([UserId], [CreatedAt] DESC);
CREATE NONCLUSTERED INDEX [IX_Notifications_Unread] ON [dbo].[Notifications] ([UserId], [IsRead])
    WHERE [IsRead] = 0;

-- =====================================================================
-- SECTION 20: AUDIT LOG (immutable — no update/delete)
-- =====================================================================

CREATE TABLE [dbo].[AuditLogs]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [UserId] NVARCHAR(450) NULL,
    [UserEmail] NVARCHAR(256) NULL,
    [Action] NVARCHAR(100) NOT NULL,
    -- e.g. "CreateProduct", "UpdateOrder"
    [EntityType] NVARCHAR(100) NOT NULL,
    -- e.g. "Product", "Order"
    [EntityId] NVARCHAR(50) NULL,
    [OldValues] NVARCHAR(MAX) NULL,
    -- JSON
    [NewValues] NVARCHAR(MAX) NULL,
    -- JSON
    [IpAddress] NVARCHAR(45) NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [Timestamp] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id])
);

CREATE NONCLUSTERED INDEX [IX_AuditLogs_UserId] ON [dbo].[AuditLogs] ([UserId], [Timestamp] DESC);
CREATE NONCLUSTERED INDEX [IX_AuditLogs_EntityType] ON [dbo].[AuditLogs] ([EntityType], [EntityId]);
CREATE NONCLUSTERED INDEX [IX_AuditLogs_Timestamp] ON [dbo].[AuditLogs] ([Timestamp] DESC);
CREATE NONCLUSTERED INDEX [IX_AuditLogs_Action] ON [dbo].[AuditLogs] ([Action])
    INCLUDE ([UserId], [EntityType], [Timestamp]);

-- =====================================================================
-- SECTION 21: SEARCH LOGS (analytics)
-- =====================================================================

CREATE TABLE [dbo].[SearchLogs]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Query] NVARCHAR(500) NOT NULL,
    [ResultCount] INT NOT NULL DEFAULT 0,
    [UserId] NVARCHAR(450) NULL,
    [Language] NVARCHAR(5) NOT NULL DEFAULT N'ar',
    [Timestamp] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_SearchLogs] PRIMARY KEY CLUSTERED ([Id])
);

CREATE NONCLUSTERED INDEX [IX_SearchLogs_Query] ON [dbo].[SearchLogs] ([Query]);
CREATE NONCLUSTERED INDEX [IX_SearchLogs_ZeroResults] ON [dbo].[SearchLogs] ([ResultCount])
    INCLUDE ([Query], [Timestamp])
    WHERE [ResultCount] = 0;
CREATE NONCLUSTERED INDEX [IX_SearchLogs_Timestamp] ON [dbo].[SearchLogs] ([Timestamp] DESC);

-- =====================================================================
-- SECTION 22: DEFERRED FOREIGN KEYS
-- =====================================================================

-- Add FK from CouponUsages to Orders (Orders table now exists)
ALTER TABLE [dbo].[CouponUsages]
    ADD CONSTRAINT [FK_CouponUsages_Order] FOREIGN KEY ([OrderId])
        REFERENCES [dbo].[Orders]([Id])
ON DELETE RESTRICT;

CREATE NONCLUSTERED INDEX [IX_CouponUsages_OrderId] ON [dbo].[CouponUsages] ([OrderId]);

GO
PRINT N'Schema creation completed successfully.';
GO

