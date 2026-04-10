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
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet("methods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _paymentService.GetPaymentMethodsAsync(userId));
        }

        [HttpPost("cards")]
        public async Task<IActionResult> AddCard([FromBody] AddCardRequest request)
        {
            var userId = User.FindFirstValue("uid");
            var result = await _paymentService.AddCardAsync(userId, request);
            return StatusCode(201, result);
        }

        [HttpDelete("cards/{id}")]
        public async Task<IActionResult> DeleteCard(string id)
        {
            var userId = User.FindFirstValue("uid");
            await _paymentService.DeleteCardAsync(userId, id);
            return Ok(new { message = "Card deleted" });
        }

        [HttpPost("intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentIntentRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _paymentService.CreatePaymentIntentAsync(userId, request));
        }
    }
}
