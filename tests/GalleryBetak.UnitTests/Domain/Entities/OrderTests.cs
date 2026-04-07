using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Enums;
using GalleryBetak.Domain.Exceptions;
using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace GalleryBetak.UnitTests.Domain.Entities
{
    public class OrderTests
    {
        private readonly Address _testAddress = Address.Create(
            "user_id", "Home", "Test Name", "01000000000", "Giza", "City", "Street"
        );

        [Fact]
        public void Create_WithValidParameters_ReturnsOrderWithCorrectState()
        {
            // Act
            var order = Order.Create("user_id", "ORD-123", _testAddress, PaymentMethod.Card);

            // Assert
            order.UserId.Should().Be("user_id");
            order.OrderNumber.Should().Be("ORD-123");
            order.Status.Should().Be(OrderStatus.Pending);
            order.PaymentStatus.Should().Be(PaymentStatus.Pending);
            order.ShippingGovernorate.Should().Be("Giza");
            order.ShippingPhone.Should().Be("01000000000");
        }

        [Fact]
        public void Confirm_ValidTransition_UpdatesStatus()
        {
            // Arrange
            var order = Order.Create("userId", "ORD-1", _testAddress, PaymentMethod.Card);

            // Act
            order.Confirm();

            // Assert
            order.Status.Should().Be(OrderStatus.Confirmed);
        }

        [Fact]
        public void Confirm_InvalidTransition_ThrowsBusinessRuleException()
        {
            // Arrange
            var order = Order.Create("userId", "ORD-1", _testAddress, PaymentMethod.Card);
            order.Confirm();
            order.StartProcessing(); // now it's Processing

            // Act
            Action act = () => order.Confirm();

            // Assert
            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void UpdatePaymentStatus_ToSuccess_UpdatesCorrectly()
        {
            // Arrange
            var order = Order.Create("userId", "ORD-1", _testAddress, PaymentMethod.Card);

            // Act
            order.UpdatePaymentStatus(PaymentStatus.Success);

            // Assert
            order.PaymentStatus.Should().Be(PaymentStatus.Success);
        }

        [Fact]
        public void DeductStock_InsufficientStock_ThrowsException()
        {
            // Moved to ProductTests but left here occasionally if testing order flows, will remove.
        }

        [Fact]
        public void RecalculateTotals_CalculatesCorrectSum()
        {
            // Arrange
            var order = Order.Create("user", "ORD-1", _testAddress, PaymentMethod.Card);
            var product = Product.Create("Product", "Product", "desc", "desc", 100m, "SKU1", 10, 1);
            order.AddItem(OrderItem.Create(product, 2)); // 200 EGP

            order.SetFinancials(50m, 10m, 20m); // Ship:50, Tax:10, Discount:20
            
            // Act
            order.RecalculateTotals();

            // Assert
            // SubTotal = 200. Total = 200 + 50 + 10 - 20 = 240
            order.SubTotal.Should().Be(200m);
            order.TotalAmount.Should().Be(240m);
        }
    }
}

