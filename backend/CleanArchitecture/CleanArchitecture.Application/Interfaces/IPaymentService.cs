using CleanArchitecture.Core.DTOs.Payment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IPaymentService
    {
        /// <summary>
        /// Initializes a payment and returns the iyzico Checkout Form token and content.
        /// </summary>
        Task<PaymentInitResponse> InitializePaymentAsync(string userId, PaymentInitRequest request, string idempotencyKey);

        /// <summary>
        /// Processes the callback from iyzico's hosted form to authorized/fail the payment.
        /// </summary>
        Task<PaymentResponse> ProcessCallbackAsync(string paymentId, string token);

        /// <summary>
        /// Captures an authorized payment (takes the money).
        /// </summary>
        Task<PaymentResponse> CapturePaymentAsync(string paymentId, int amount);

        /// <summary>
        /// Voids or refunds a payment based on its current state.
        /// </summary>
        Task<PaymentResponse> CancelPaymentAsync(string paymentId, string reason);

        /// <summary>
        /// Get a single payment by its ID.
        /// </summary>
        Task<PaymentResponse> GetPaymentByIdAsync(string paymentId);

        /// <summary>
        /// Get all payment attempts for a specific order.
        /// </summary>
        Task<List<PaymentResponse>> GetPaymentsByOrderIdAsync(string orderId);
    }
}
