using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.DTOs.Review;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/orders")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("checkout/preview")]
        public async Task<IActionResult> CheckoutPreview([FromBody] CheckoutPreviewRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _orderService.GetCheckoutPreviewAsync(userId, request));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var userId = User.FindFirstValue("uid");
            var result = await _orderService.CreateOrderAsync(userId, request);
            return StatusCode(201, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _orderService.GetOrdersAsync(userId, page, limit));
        }

        [HttpGet("{order_id}")]
        public async Task<IActionResult> GetOrder(string order_id)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _orderService.GetOrderByIdAsync(userId, order_id));
        }

        [HttpPost("{order_id}/rating")]
        public async Task<IActionResult> SubmitRating(string order_id, [FromBody] SubmitRatingRequest request)
        {
            var userId = User.FindFirstValue("uid");
            await _orderService.SubmitRatingAsync(userId, order_id, request);
            return Ok(new { message = "Review submitted successfully" });
        }
    }
}
