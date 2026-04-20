using CleanArchitecture.Core.DTOs.Payment;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Infrastructure.Services;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.UnitTests.Services
{
    public class PaymentServiceTests
    {
        private readonly PaymentService _paymentService;

        public PaymentServiceTests()
        {
            _paymentService = new PaymentService();
        }

        [Fact]
        public async Task InitializePayment_WithValidRequest_ShouldReturnAwaitingForm()
        {
            // Arrange
            var userId = "user_123";
            var request = CreateSampleRequest();
            var idemKey = Guid.NewGuid().ToString();

            // Act
            var result = await _paymentService.InitializePaymentAsync(userId, request, idemKey);

            // Assert
            result.Payment.Status.Should().Be("AWAITING_FORM");
            result.CheckoutForm.Token.Should().NotBeNullOrEmpty();
            result.CheckoutForm.Content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task InitializePayment_DuplicateIdempotency_ShouldReturnSameResponse()
        {
            // Arrange
            var userId = "user_123";
            var request = CreateSampleRequest();
            var idemKey = "idem_123";

            // Act
            var first = await _paymentService.InitializePaymentAsync(userId, request, idemKey);
            var second = await _paymentService.InitializePaymentAsync(userId, request, idemKey);

            // Assert
            first.Payment.Id.Should().Be(second.Payment.Id);
            first.CheckoutForm.Token.Should().Be(second.CheckoutForm.Token);
        }

        [Fact]
        public async Task FullLifecycle_ShouldTransitionCorrectly()
        {
            // Arrange
            var userId = "user_123";
            var request = CreateSampleRequest();
            var idemKey = Guid.NewGuid().ToString();

            // 1. Initialized
            var init = await _paymentService.InitializePaymentAsync(userId, request, idemKey);
            var payId = init.Payment.Id;
            var token = init.CheckoutForm.Token;

            // 2. Callback
            var authorized = await _paymentService.ProcessCallbackAsync(payId, token);
            authorized.Status.Should().Be("AUTHORIZED");

            // 3. Capture
            var captured = await _paymentService.CapturePaymentAsync(payId, 15000);
            captured.Status.Should().Be("CAPTURED");
        }

        [Fact]
        public async Task Capture_WithWrongAmount_ShouldThrow()
        {
            // Arrange
            var userId = "user_123";
            var request = CreateSampleRequest();
            var init = await _paymentService.InitializePaymentAsync(userId, request, Guid.NewGuid().ToString());
            await _paymentService.ProcessCallbackAsync(init.Payment.Id, init.CheckoutForm.Token);

            // Act
            Func<Task> act = async () => await _paymentService.CapturePaymentAsync(init.Payment.Id, 10000);

            // Assert
            await act.Should().ThrowAsync<ApiException>().Where(e => e.ErrorCode == "AMOUNT_MISMATCH");
        }

        [Fact]
        public async Task Cancel_Authorized_ShouldVoid()
        {
            // Arrange
            var init = await _paymentService.InitializePaymentAsync("u", CreateSampleRequest(), Guid.NewGuid().ToString());
            await _paymentService.ProcessCallbackAsync(init.Payment.Id, init.CheckoutForm.Token);

            // Act
            var cancelled = await _paymentService.CancelPaymentAsync(init.Payment.Id, "user_cancelled");

            // Assert
            cancelled.Status.Should().Be("VOIDED");
            cancelled.CancelReason.Should().Be("user_cancelled");
        }

        private PaymentInitRequest CreateSampleRequest() => new PaymentInitRequest
        {
            OrderId = "ord_123",
            Amount = 15000,
            Currency = "TRY",
            Buyer = new BuyerDto { Email = "test@example.com" },
            Items = new List<PaymentItemDto> { new PaymentItemDto { Name = "Pizza", Price = "150.00" } }
        };
    }
}
