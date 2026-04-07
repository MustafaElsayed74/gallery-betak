using AutoMapper;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Order;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Enums;
using GalleryBetak.Domain.Interfaces;
using GalleryBetak.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GalleryBetak.UnitTests.Application.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICartService> _mockCartService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IGenericRepository<Address>> _mockAddressRepo;
        private readonly Mock<IGenericRepository<Cart>> _mockCartRepo;
        private readonly Mock<IOrderRepository> _mockOrderRepo;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCartService = new Mock<ICartService>();
            _mockMapper = new Mock<IMapper>();
            _mockAddressRepo = new Mock<IGenericRepository<Address>>();
            _mockCartRepo = new Mock<IGenericRepository<Cart>>();
            _mockOrderRepo = new Mock<IOrderRepository>();
            _mockUserManager = CreateUserManagerMock();

            _mockUow.Setup(u => u.Addresses).Returns(_mockAddressRepo.Object);
            _mockUow.Setup(u => u.Carts).Returns(_mockCartRepo.Object);
            _mockUow.Setup(u => u.Orders).Returns(_mockOrderRepo.Object);

            _mockUserManager
                .Setup(manager => manager.GetUsersInRoleAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<ApplicationUser>());

            _orderService = new OrderService(_mockUow.Object, _mockCartService.Object, _mockMapper.Object, _mockUserManager.Object);
        }

        private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);
        }

        [Fact]
        public async Task CreateOrderAsync_ValidRequest_Success()
        {
            // Arrange
            var userId = "user1";
            var request = new CreateOrderRequest { AddressId = 1, PaymentMethod = PaymentMethod.Card };
            var address = Address.Create(userId, "Home", "Recipient", "01000000000", "Giza", "City", "Street");
            var cart = Cart.CreateForUser(userId);
            var product = Product.Create("P1", "P1", "d", "d", 100m, "SKU1", 10, 1);
            cart.AddItem(product.Id, product.Price, 2);
            // Manually set product on cart item because logic expects it
            cart.Items.First().GetType().GetProperty("Product")?.SetValue(cart.Items.First(), product);

            _mockAddressRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(address);
            _mockCartRepo.Setup(r => r.GetEntityWithSpecAsync(It.IsAny<ISpecification<Cart>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cart);
            
            // Mock the reload and mapping
            _mockOrderRepo.Setup(r => r.GetEntityWithSpecAsync(It.IsAny<ISpecification<Order>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Order.Create(userId, "ORD-1", address, PaymentMethod.Card));

            // Act
            var response = await _orderService.CreateOrderAsync(userId, request);

            // Assert
            response.Success.Should().BeTrue();
            cart.IsEmpty.Should().BeTrue();
            product.StockQuantity.Should().Be(8);
            _mockOrderRepo.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_ConfirmsOrder_Success()
        {
            // Arrange
            var address = Address.Create("u1", "H", "R", "01000000000", "G", "C", "S");
            var order = Order.Create("u1", "ORD-1", address, PaymentMethod.Card);
            _mockOrderRepo.Setup(r => r.GetEntityWithSpecAsync(It.IsAny<ISpecification<Order>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            // Act
            var response = await _orderService.UpdateOrderStatusAsync(1, OrderStatus.Confirmed);

            // Assert
            response.Success.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.Confirmed);
            _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_InvalidTransition_ReturnsFail()
        {
            // Arrange
            var address = Address.Create("u1", "H", "R", "01000000000", "G", "C", "S");
            var order = Order.Create("u1", "ORD-1", address, PaymentMethod.Card);
            order.Confirm();
            order.StartProcessing(); // Now Processing
            
            _mockOrderRepo.Setup(r => r.GetEntityWithSpecAsync(It.IsAny<ISpecification<Order>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            // Act - Try to confirm again (Processing -> Confirmed is invalid)
            var response = await _orderService.UpdateOrderStatusAsync(1, OrderStatus.Confirmed);

            // Assert
            response.Success.Should().BeFalse();
            response.MessageEn.Should().Contain("Invalid transition");
        }
    }
}

