using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Retrieves the current user's order history with pagination and status filtering.
        /// </summary>
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders([FromQuery] string status, [FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _orderService.GetMyOrdersAsync(userId, status, page, size));
        }

        /// <summary>
        /// Retrieves detailed information about a specific order.
        /// </summary>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(string id)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _orderService.GetOrderByIdAsync(userId, id));
        }

        /// <summary>
        /// Cancels a pending or held order.
        /// </summary>
        [Authorize]
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(string id)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _orderService.CancelOrderAsync(userId, id));
        }

        /// <summary>
        /// Re-populates the basket with items from a previous order.
        /// </summary>
        [Authorize]
        [HttpPost("{id}/reorder")]
        public async Task<IActionResult> Reorder(string id)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _orderService.ReorderAsync(userId, id));
        }

        /// <summary>
        /// Initiates a refund request for a paid or delivered order.
        /// </summary>
        [Authorize]
        [HttpPost("{id}/request-refund")]
        public async Task<IActionResult> RequestRefund(string id)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _orderService.RequestRefundAsync(userId, id));
        }
    }
}
