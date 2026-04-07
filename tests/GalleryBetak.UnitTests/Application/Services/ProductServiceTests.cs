using AutoMapper;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Product;
using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Interfaces;
using GalleryBetak.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GalleryBetak.UnitTests.Application.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IProductRepository> _mockProductRepo;
        private readonly Mock<ICategoryRepository> _mockCategoryRepo;
        private readonly Mock<IGenericRepository<Tag>> _mockTagRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<ProductService>> _mockLogger;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockProductRepo = new Mock<IProductRepository>();
            _mockCategoryRepo = new Mock<ICategoryRepository>();
            _mockTagRepo = new Mock<IGenericRepository<Tag>>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<ProductService>>();

            _mockUow.Setup(u => u.Products).Returns(_mockProductRepo.Object);
            _mockUow.Setup(u => u.Categories).Returns(_mockCategoryRepo.Object);
            _mockUow.Setup(u => u.Tags).Returns(_mockTagRepo.Object);

            _productService = new ProductService(_mockUow.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetProductsAsync_ReturnsPagedResult()
        {
            // Arrange
            var query = new ProductQueryParams { PageNumber = 1, PageSize = 10 };
            var products = new List<Product>
            {
                Product.Create("Product 1", "Product 1", "desc", "desc", 100m, "SKU1", 10, 1)
            };
            
            _mockProductRepo.Setup(r => r.ListAsync(It.IsAny<ISpecification<Product>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);
            _mockProductRepo.Setup(r => r.CountAsync(It.IsAny<ISpecification<Product>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mockMapper.Setup(m => m.Map<IReadOnlyList<ProductListDto>>(products))
                .Returns(new List<ProductListDto> { new ProductListDto { Id = 1, NameAr = "Product 1" } });

            // Act
            var response = await _productService.GetProductsAsync(query);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.TotalCount.Should().Be(1);
            response.Data.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingProduct_ReturnsSuccess()
        {
            // Arrange
            var product = Product.Create("P1", "P1", "d", "d", 100m, "SKU1", 10, 1);
            _mockProductRepo.Setup(r => r.GetEntityWithSpecAsync(It.IsAny<ISpecification<Product>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            
            _mockMapper.Setup(m => m.Map<ProductDetailDto>(product))
                .Returns(new ProductDetailDto { Id = 1, NameAr = "P1" });

            // Act
            var response = await _productService.GetByIdAsync(1);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingProduct_ReturnsNotFound()
        {
            // Arrange
            _mockProductRepo.Setup(r => r.GetEntityWithSpecAsync(It.IsAny<ISpecification<Product>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product)null!);

            // Act
            var response = await _productService.GetByIdAsync(999);

            // Assert
            response.Success.Should().BeFalse();
            response.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreateAsync_DuplicateSKU_ReturnsConflict()
        {
            // Arrange
            var request = new CreateProductRequest { SKU = "EXISTING" };
            _mockProductRepo.Setup(r => r.GetBySKUAsync("EXISTING", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Product.Create("P", "P", "d", "d", 10m, "EXISTING", 1, 1));

            // Act
            var response = await _productService.CreateAsync(request);

            // Assert
            response.Success.Should().BeFalse();
            response.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task DeleteAsync_ExistingProduct_ReturnsSuccess()
        {
            // Arrange
            var product = Product.Create("P", "P", "d", "d", 10m, "SKU", 1, 1);
            _mockProductRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            // Act
            var response = await _productService.DeleteAsync(1);

            // Assert
            response.Success.Should().BeTrue();
            product.IsDeleted.Should().BeTrue(); // BaseEntity property
            _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

