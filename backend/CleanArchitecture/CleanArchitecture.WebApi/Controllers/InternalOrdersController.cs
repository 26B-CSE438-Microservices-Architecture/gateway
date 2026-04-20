using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/internal/orders")]
    [ApiController]
    public class InternalOrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private const string InternalSecret = "change-me-in-production";

        public InternalOrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Recieves payment status updates (e.g. HOLD_CONFIRMED) from the Payment Service.
        /// Requires X-Internal-Secret header.
        /// </summary>
        [HttpPost("{id}/payment-callback")]
        public async Task<IActionResult> PaymentCallback(string id, [FromBody] InternalPaymentCallbackRequest request)
        {
            // Simple X-Internal-Secret validation as per README
            var secret = Request.Headers["X-Internal-Secret"].ToString();
            if (string.IsNullOrEmpty(secret) || secret != InternalSecret)
            {
                return Unauthorized(new { error = "UNAUTHORIZED", message = "Invalid or missing X-Internal-Secret header." });
            }

            var result = await _orderService.ProcessPaymentCallbackAsync(id, request.Status);
            return Ok(result);
        }
    }
}
