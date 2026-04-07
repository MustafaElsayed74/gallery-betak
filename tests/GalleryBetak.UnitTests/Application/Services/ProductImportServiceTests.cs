using FluentAssertions;
using GalleryBetak.Application.DTOs.Product;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Infrastructure.Services;
using GalleryBetak.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GalleryBetak.UnitTests.Application.Services;

public class ProductImportServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly Mock<IPhotoService> _photoService = new();
    private readonly Mock<ILogger<ProductImportService>> _logger = new();

    [Fact]
    public async Task ImportFromUrlAsync_LocalhostUrl_ReturnsBadRequest()
    {
        // Arrange
        var service = CreateService(new ProductImporterSettings());

        // Act
        var result = await service.ImportFromUrlAsync(new ProductImportRequest
        {
            Url = "http://localhost:5000/product"
        });

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ImportFromUrlAsync_DomainOutsideAllowlist_ReturnsForbidden()
    {
        // Arrange
        var settings = new ProductImporterSettings
        {
            AllowedDomains = ["allowed-shop.com"]
        };

        var service = CreateService(settings);

        // Act
        var result = await service.ImportFromUrlAsync(new ProductImportRequest
        {
            Url = "https://another-shop.com/product/123"
        });

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    private ProductImportService CreateService(ProductImporterSettings settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        return new ProductImportService(
            _httpClientFactory.Object,
            _photoService.Object,
            new ProductImportHtmlParser(),
            Options.Create(settings),
            configuration,
            _logger.Object);
    }
}
