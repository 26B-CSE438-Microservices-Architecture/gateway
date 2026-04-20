using CleanArchitecture.Core.DTOs.Payment;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Initialize a payment and get the iyzico Checkout Form.
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> InitializePayment([FromBody] PaymentInitRequest request)
        {
            var userId = User.FindFirstValue("uid");
            var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
            
            var result = await _paymentService.InitializePaymentAsync(userId, request, idempotencyKey);
            
            // Per README: A failed payment still returns 201 because the record was created.
            return CreatedAtAction(nameof(GetPayment), new { id = result.Payment.Id }, result);
        }

        /// <summary>
        /// Callback called after user completes the iyzico hosted form.
        /// </summary>
        [HttpPost("{id}/checkout-form/callback")]
        public async Task<IActionResult> ProcessCallback(string id, [FromBody] PaymentCallbackRequest request)
        {
            var result = await _paymentService.ProcessCallbackAsync(id, request.Token);
            return Ok(new { payment = result });
        }

        /// <summary>
        /// Capture funds for an authorized payment.
        /// </summary>
        [Authorize]
        [HttpPost("{id}/capture")]
        public async Task<IActionResult> CapturePayment(string id, [FromBody] PaymentCaptureRequest request)
        {
            var result = await _paymentService.CapturePaymentAsync(id, request.Amount);
            return Ok(new { payment = result });
        }

        /// <summary>
        /// Cancel (Void or Refund) a payment.
        /// </summary>
        [Authorize]
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelPayment(string id, [FromBody] PaymentCancelRequest request)
        {
            var result = await _paymentService.CancelPaymentAsync(id, request.Reason);
            return Ok(new { payment = result });
        }

        /// <summary>
        /// Get payment details by ID.
        /// </summary>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(string id)
        {
            var result = await _paymentService.GetPaymentByIdAsync(id);
            return Ok(new { payment = result });
        }

        /// <summary>
        /// Get all payments for an order.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetPaymentsByOrder([FromQuery] string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
                return BadRequest(new { error = "INVALID_REQUEST", message = "orderId query parameter is required." });

            var results = await _paymentService.GetPaymentsByOrderIdAsync(orderId);
            return Ok(new { payments = results });
        }
    }
}
