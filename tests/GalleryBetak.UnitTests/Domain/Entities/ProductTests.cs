using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Exceptions;
using FluentAssertions;
using System;
using Xunit;

namespace GalleryBetak.UnitTests.Domain.Entities
{
    public class ProductTests
    {
        [Fact]
        public void Create_ValidParameters_ReturnsProduct()
        {
            // Act
            var product = Product.Create("Arabic Name", "English Name", "desc ar", "desc en", 100m, "SKU-1", 10, 1);

            // Assert
            product.NameAr.Should().Be("Arabic Name");
            product.Price.Should().Be(100m);
            product.SKU.Should().Be("SKU-1");
            product.StockQuantity.Should().Be(10);
            product.CategoryId.Should().Be(1);
            product.Slug.Should().Be("english-name");
        }

        [Fact]
        public void DeductStock_ValidQuantity_ReducesStock()
        {
            // Arrange
            var product = Product.Create("Arabic", "English", "desc", "desc", 100m, "SKU", 10, 1);

            // Act
            product.DeductStock(3);

            // Assert
            product.StockQuantity.Should().Be(7);
        }

        [Fact]
        public void DeductStock_ExceedsQuantity_ThrowsException()
        {
            // Arrange
            var product = Product.Create("Arabic", "English", "desc", "desc", 100m, "SKU", 10, 1);

            // Act
            Action act = () => product.DeductStock(15);

            // Assert
            act.Should().Throw<InsufficientStockException>();
        }

        [Fact]
        public void ApplyDiscount_CalculatesPercentageCorrectly()
        {
            // Arrange
            var product = Product.Create("Arabic", "English", "desc", "desc", 80m, "SKU", 10, 1);

            // Act (Price is 80, Original is 100 -> 20% discount)
            product.SetDiscount(100m);

            // Assert
            product.HasDiscount.Should().BeTrue();
            product.DiscountPercentage.Should().Be(20);
        }

        [Fact]
        public void RemoveDiscount_ClearsOriginalPrice()
        {
            // Arrange
            var product = Product.Create("Arabic", "English", "desc", "desc", 80m, "SKU", 10, 1);
            product.SetDiscount(100m);

            // Act
            product.RemoveDiscount();

            // Assert
            product.OriginalPrice.Should().BeNull();
            product.HasDiscount.Should().BeFalse();
        }

        [Fact]
        public void Create_WithUnsafeCharactersInName_GeneratesUrlSafeSlug()
        {
            // Act
            var product = Product.Create(
                "زجاجة مياه",
                "Bottle 500/750 ml : Amazon.eg",
                "desc ar",
                "desc en",
                150m,
                "SKU-500-750",
                5,
                1);

            // Assert
            product.Slug.Should().Be("bottle-500-750-ml-amazon-eg");
            product.Slug.Should().NotContain("/");
            product.Slug.Should().NotContain(":");
        }

        [Fact]
        public void Create_WithNonLatinOnlyName_FallsBackToArabicOrSkuSlug()
        {
            // Act
            var product = Product.Create(
                "سماعة أذن",
                "@@@",
                "desc ar",
                "desc en",
                120m,
                "R3501",
                8,
                1);

            // Assert
            product.Slug.Should().NotBeNullOrWhiteSpace();
            product.Slug.Should().NotContain("/");
        }
    }
}

