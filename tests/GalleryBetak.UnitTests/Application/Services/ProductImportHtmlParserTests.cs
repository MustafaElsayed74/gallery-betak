using FluentAssertions;
using GalleryBetak.Infrastructure.Services;
using Xunit;

namespace GalleryBetak.UnitTests.Application.Services;

public class ProductImportHtmlParserTests
{
    private readonly ProductImportHtmlParser _parser = new();

    [Fact]
    public void Extract_WithJsonLdProduct_ReturnsStructuredFields()
    {
        // Arrange
        const string html = """
            <html>
              <head>
                <script type="application/ld+json">
                {
                  "@context": "https://schema.org",
                  "@type": "Product",
                  "name": "Handmade Vase",
                  "description": "Decorative ceramic vase",
                  "sku": "VASE-01",
                  "image": ["https://cdn.example.com/vase-1.jpg", "https://cdn.example.com/vase-2.jpg"],
                  "offers": {
                    "@type": "Offer",
                    "price": "799.50",
                    "priceCurrency": "EGP"
                  }
                }
                </script>
              </head>
              <body></body>
            </html>
            """;

        // Act
        var result = _parser.Extract(html, new Uri("https://example.com/products/vase"));

        // Assert
        result.Name.Should().Be("Handmade Vase");
        result.Description.Should().Be("Decorative ceramic vase");
        result.Sku.Should().Be("VASE-01");
        result.Price.Should().Be(799.50m);
        result.Currency.Should().Be("EGP");
        result.ImageUrls.Should().Contain("https://cdn.example.com/vase-1.jpg");
        result.ImageUrls.Should().Contain("https://cdn.example.com/vase-2.jpg");
    }

    [Fact]
    public void Extract_WithMetaFallback_ResolvesRelativeImageUrls()
    {
        // Arrange
        const string html = """
            <html>
              <head>
                <title>Decor Lamp</title>
                <meta property="og:title" content="Decor Lamp" />
                <meta property="og:description" content="Modern lamp for living room" />
                <meta property="product:price:amount" content="1200" />
                <meta property="product:price:currency" content="EGP" />
              </head>
              <body>
                <img src="/images/lamp-main.jpg" />
              </body>
            </html>
            """;

        // Act
        var result = _parser.Extract(html, new Uri("https://shop.example.com/item/lamp"));

        // Assert
        result.Name.Should().Be("Decor Lamp");
        result.Description.Should().Be("Modern lamp for living room");
        result.Price.Should().Be(1200m);
        result.Currency.Should().Be("EGP");
        result.ImageUrls.Should().Contain("https://shop.example.com/images/lamp-main.jpg");
    }

    [Fact]
    public void Extract_FiltersDecorativeImages_AndKeepsProductImage()
    {
        // Arrange
        const string html = """
            <html>
              <head>
                <meta property="og:title" content="Wooden Chair" />
              </head>
              <body>
                <img src="/assets/logo.png" class="site-logo" width="120" height="40" />
                <img src="/assets/icons/cart.svg" class="menu-icon" />
                <img src="/products/chair-main.jpg" class="product-gallery-image" width="900" height="900" />
                <img src="/products/chair-thumb.jpg" class="product-thumb" width="80" height="80" />
              </body>
            </html>
            """;

        // Act
        var result = _parser.Extract(html, new Uri("https://shop.example.com/item/chair"));

        // Assert
        result.ImageUrls.Should().Contain("https://shop.example.com/products/chair-main.jpg");
        result.ImageUrls.Should().NotContain(url => url.Contains("logo", StringComparison.OrdinalIgnoreCase));
        result.ImageUrls.Should().NotContain(url => url.EndsWith(".svg", StringComparison.OrdinalIgnoreCase));
        result.ImageUrls.Should().NotContain(url => url.Contains("thumb", StringComparison.OrdinalIgnoreCase));
    }
}
