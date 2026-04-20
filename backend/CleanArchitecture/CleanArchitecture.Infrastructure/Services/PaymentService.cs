using CleanArchitecture.Core.DTOs.Payment;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        // In-memory storage for payments
        private static readonly ConcurrentDictionary<string, PaymentResponse> _payments = new();
        // In-memory storage for idempotency: Key -> Response
        private static readonly ConcurrentDictionary<string, PaymentInitResponse> _idempotencyResponses = new();
        // Storage for form details
        private static readonly ConcurrentDictionary<string, CheckoutFormDetails> _formDetails = new();

        public Task<PaymentInitResponse> InitializePaymentAsync(string userId, PaymentInitRequest request, string idempotencyKey)
        {
            if (string.IsNullOrEmpty(idempotencyKey))
                throw new ApiException("MISSING_IDEMPOTENCY_KEY", "Idempotency-Key header is required.");

            // 1. Idempotency Check
            if (_idempotencyResponses.TryGetValue(idempotencyKey, out var existingResponse))
            {
                return Task.FromResult(existingResponse);
            }

            // 2. Void any existing AUTHORIZED payments for the same order (as per README retry behavior)
            var existingAuthorized = _payments.Values
                .Where(p => p.OrderId == request.OrderId && p.Status == "AUTHORIZED")
                .ToList();
            
            foreach (var authPayment in existingAuthorized)
            {
                authPayment.Status = "VOIDED";
                authPayment.CancelledAt = DateTime.UtcNow;
                authPayment.CancelReason = "new_payment_attempt";
            }

            // 3. Create New Payment
            var paymentId = $"pay_{Guid.NewGuid():N}".Substring(0, 16);
            var payment = new PaymentResponse
            {
                Id = paymentId,
                OrderId = request.OrderId,
                UserId = userId,
                Status = "AWAITING_FORM",
                Amount = request.Amount,
                Currency = request.Currency,
                Provider = "iyzico",
                CreatedAt = DateTime.UtcNow
            };

            // 4. Create Mock Checkout Form
            var token = $"cf_token_{Guid.NewGuid():N}".Substring(0, 12);
            var mockHtml = $"<html><body><h1>Iyzico Checkout Form Mock</h1><p>Order: {request.OrderId}</p><p>Amount: {request.Amount / 100.0} {request.Currency}</p></body></html>";
            var base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(mockHtml));

            var form = new CheckoutFormDetails
            {
                Token = token,
                Content = base64Html,
                PaymentPageUrl = $"https://sandbox-api.iyzipay.com/mock-page/{token}"
            };

            var result = new PaymentInitResponse
            {
                Payment = payment,
                CheckoutForm = form
            };

            // 5. Save to memory
            _payments[paymentId] = payment;
            _idempotencyResponses[idempotencyKey] = result;
            _formDetails[paymentId] = form;

            return Task.FromResult(result);
        }

        public Task<PaymentResponse> ProcessCallbackAsync(string paymentId, string token)
        {
            if (!_payments.TryGetValue(paymentId, out var payment))
                throw new NotFoundException("PAYMENT_NOT_FOUND", $"No payment found with id {paymentId}");

            if (!_formDetails.TryGetValue(paymentId, out var form) || form.Token != token)
                throw new ApiException("MISSING_FORM_TOKEN", "Invalid or missing checkout form token.");

            if (payment.Status != "AWAITING_FORM")
                throw new ApiException("INVALID_STATE_TRANSITION", $"Action not allowed in current state: {payment.Status}");

            // Mock logic: Success if token ends with even char, fail if odd (simple deterministic mock)
            // Or just always success for this base mock. Let's make it always success for now.
            payment.Status = "AUTHORIZED";
            payment.AuthorizedAt = DateTime.UtcNow;
            payment.ProviderTxId = $"tx_{Guid.NewGuid():N}".Substring(0, 8);

            return Task.FromResult(payment);
        }

        public Task<PaymentResponse> CapturePaymentAsync(string paymentId, int amount)
        {
            if (!_payments.TryGetValue(paymentId, out var payment))
                throw new NotFoundException("PAYMENT_NOT_FOUND", $"No payment found with id {paymentId}");

            if (payment.Status != "AUTHORIZED")
                throw new ApiException("INVALID_STATE_TRANSITION", "Only AUTHORIZED payments can be captured.");

            if (payment.Amount != amount)
                throw new ApiException("AMOUNT_MISMATCH", "Capture amount must match the fully authorized amount.");

            payment.Status = "CAPTURED";
            payment.CapturedAt = DateTime.UtcNow;

            return Task.FromResult(payment);
        }

        public Task<PaymentResponse> CancelPaymentAsync(string paymentId, string reason)
        {
            if (!_payments.TryGetValue(paymentId, out var payment))
                throw new NotFoundException("PAYMENT_NOT_FOUND", $"No payment found with id {paymentId}");

            if (payment.Status == "AUTHORIZED")
            {
                payment.Status = "VOIDED";
            }
            else if (payment.Status == "CAPTURED")
            {
                payment.Status = "REFUNDED";
            }
            else
            {
                throw new ApiException("INVALID_STATE_TRANSITION", $"Cannot cancel payment in {payment.Status} state.");
            }

            payment.CancelledAt = DateTime.UtcNow;
            payment.CancelReason = reason;

            return Task.FromResult(payment);
        }

        public Task<PaymentResponse> GetPaymentByIdAsync(string paymentId)
        {
            if (!_payments.TryGetValue(paymentId, out var payment))
                throw new NotFoundException("PAYMENT_NOT_FOUND", $"No payment found with id {paymentId}");

            return Task.FromResult(payment);
        }

        public Task<List<PaymentResponse>> GetPaymentsByOrderIdAsync(string orderId)
        {
            var results = _payments.Values
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return Task.FromResult(results);
        }
    }
}
