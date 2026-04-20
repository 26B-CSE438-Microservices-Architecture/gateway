using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Infrastructure.Services;
using FluentAssertions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.UnitTests.Services
{
    public class OrderServiceTests
    {
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _orderService = new OrderService();
        }

        [Fact]
        public async Task Cart_FullFlow_ShouldWork()
        {
            // Arrange
            var userId = "user_123";
            
            // 1. Add item
            await _orderService.AddToCartAsync(userId, new AddCartItemRequest { ProductId = "p_1", Quantity = 2 });
            var cart = await _orderService.GetCartAsync(userId);
            cart.Items.Should().HaveCount(1);
            cart.TotalAmount.Should().Be(240.0); // 120 * 2

            // 2. Update item
            await _orderService.UpdateCartItemAsync(userId, "p_1", new UpdateCartItemRequest { Quantity = 3 });
            cart = await _orderService.GetCartAsync(userId);
            cart.TotalAmount.Should().Be(360.0); // 120 * 3

            // 3. Clear cart
            await _orderService.ClearCartAsync(userId);
            cart = await _orderService.GetCartAsync(userId);
            cart.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Checkout_ShouldCreateOrderAndClearCart()
        {
            // Arrange
            var userId = "user_checkout";
            await _orderService.AddToCartAsync(userId, new AddCartItemRequest { ProductId = "p_2", Quantity = 1 });
            var checkoutRequest = new CheckoutRequest 
            { 
                DeliveryAddress = new OrderAddressDto { Street = "Main St" } 
            };

            // Act
            var order = await _orderService.CheckoutAsync(userId, checkoutRequest, Guid.NewGuid().ToString());

            // Assert
            order.Status.Should().Be("PAYMENT_PENDING");
            order.TotalAmount.Should().Be(180.0);
            
            var cart = await _orderService.GetCartAsync(userId);
            cart.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task PaymentSaga_ShouldTransitionStates()
        {
            // Arrange
            var userId = "user_saga";
            await _orderService.AddToCartAsync(userId, new AddCartItemRequest { ProductId = "p_3", Quantity = 1 });
            var order = await _orderService.CheckoutAsync(userId, new CheckoutRequest(), Guid.NewGuid().ToString());
            var orderId = order.OrderId;

            // 1. Hold Confirmed
            await _orderService.ProcessPaymentCallbackAsync(orderId, "HOLD_CONFIRMED");
            order = await _orderService.GetOrderByIdAsync(userId, orderId);
            order.Status.Should().Be("PAYMENT_HELD");

            // 2. Restaurant Confirm (trigger Capture)
            await _orderService.ConfirmOrderAsync("rest_1", orderId);
            order = await _orderService.GetOrderByIdAsync(userId, orderId);
            order.Status.Should().Be("PAID");
        }

        [Fact]
        public async Task Cancel_PaidOrder_ShouldThrow()
        {
            // Arrange
            var userId = "user_cancel";
            await _orderService.AddToCartAsync(userId, new AddCartItemRequest { ProductId = "p_1", Quantity = 1 });
            var order = await _orderService.CheckoutAsync(userId, new CheckoutRequest(), Guid.NewGuid().ToString());
            await _orderService.ProcessPaymentCallbackAsync(order.OrderId, "HOLD_CONFIRMED");
            await _orderService.ConfirmOrderAsync("rest_1", order.OrderId); // This makes it PAID

            // Act
            Func<Task> act = async () => await _orderService.CancelOrderAsync(userId, order.OrderId);

            // Assert
            // In our mock, PAID orders can't be 'cancelled' easily (should use refund)
            await act.Should().ThrowAsync<ApiException>();
        }
    }
}
