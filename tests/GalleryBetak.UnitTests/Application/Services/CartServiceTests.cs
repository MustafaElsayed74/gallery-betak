using AutoMapper;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Cart;
using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Interfaces;
using GalleryBetak.Infrastructure.Services;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GalleryBetak.UnitTests.Application.Services
{
    public class CartServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IGenericRepository<Cart>> _mockCartRepo;
        private readonly Mock<IProductRepository> _mockProductRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly CartService _cartService;

        public CartServiceTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCartRepo = new Mock<IGenericRepository<Cart>>();
            _mockProductRepo = new Mock<IProductRepository>();
            _mockMapper = new Mock<IMapper>();

            _mockUow.Setup(u => u.Carts).Returns(_mockCartRepo.Object);
            _mockUow.Setup(u => u.Products).Returns(_mockProductRepo.Object);

            _cartService = new CartService(_mockUow.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task AddToCartAsync_ValidProduct_AddsItemSuccessfully()
        {
            // Arrange
            var userId = "user1";
            var request = new AddToCartRequest { ProductId = 1, Quantity = 2 };
            var product = Product.Create("P1", "P1", "d", "d", 100m, "SKU1", 10, 1);
            var cart = Cart.CreateForUser(userId);

            _mockProductRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            
            _mockCartRepo.Setup(r => r.GetEntityWithSpecAsync(It.IsAny<ISpecification<Cart>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cart);

            // Act
            var response = await _cartService.AddToCartAsync(userId, null, request);

            // Assert
            response.Success.Should().BeTrue();
            cart.Items.Should().HaveCount(1);
            cart.Items.First().Quantity.Should().Be(2);
            _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddToCartAsync_InsufficientStock_ReturnsBadRequest()
        {
            // Arrange
            var userId = "user1";
            var request = new AddToCartRequest { ProductId = 1, Quantity = 20 };
            var product = Product.Create("P1", "P1", "d", "d", 100m, "SKU1", 5, 1);
            var cart = Cart.CreateForUser(userId);

            _mockProductRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            
            _mockCartRepo.Setup(r => r.GetEntityWithSpecAsync(It.IsAny<ISpecification<Cart>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cart);

            // Act
            var response = await _cartService.AddToCartAsync(userId, null, request);

            // Assert
            response.Success.Should().BeFalse();
            response.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task MergeCartAsync_GuestToUser_MergesCorrectly()
        {
            // Arrange
            var userId = "user1";
            var sessionId = "session1";
            
            var guestCart = Cart.CreateForGuest(sessionId);
            guestCart.AddItem(1, 100m, 2);
            
            var userCart = Cart.CreateForUser(userId);
            userCart.AddItem(2, 50m, 1);

            // Setup sequencing of calls to GetEntityWithSpecAsync
            _mockCartRepo.SetupSequence(r => r.GetEntityWithSpecAsync(It.IsAny<ISpecification<Cart>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(guestCart)
                .ReturnsAsync(userCart);

            // Act
            var response = await _cartService.MergeCartAsync(userId, sessionId);

            // Assert
            response.Success.Should().BeTrue();
            userCart.Items.Should().HaveCount(2);
            _mockCartRepo.Verify(r => r.Remove(guestCart), Times.Once);
            _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

