using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/orders/restaurant")]
    [ApiController]
    [Authorize(Roles = "RestaurantOwner,restaurant_owner")]
    public class RestaurantOrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public RestaurantOrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Retrieves paginated orders belonging to the restaurant.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRestaurantOrders([FromQuery] string status, [FromQuery] int page = 0, [FromQuery] int size = 20)
        {
            var restaurantId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(restaurantId)) return Unauthorized();
            return Ok(await _orderService.GetRestaurantOrdersAsync(restaurantId, status, page, size));
        }

        /// <summary>
        /// Confirms a held order, triggering the payment capture process.
        /// </summary>
        [HttpPatch("{id}/confirm")]
        public async Task<IActionResult> ConfirmOrder(string id)
        {
            var restaurantId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(restaurantId)) return Unauthorized();
            return Ok(await _orderService.ConfirmOrderAsync(restaurantId, id));
        }

        /// <summary>
        /// Rejects a held order, triggering a payment hold release.
        /// </summary>
        [HttpPatch("{id}/reject")]
        public async Task<IActionResult> RejectOrder(string id)
        {
            var restaurantId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(restaurantId)) return Unauthorized();
            return Ok(await _orderService.RejectOrderAsync(restaurantId, id));
        }

        /// <summary>
        /// Manually updates the order status (e.g. PREPARING, DELIVERED).
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateOrderStatusRequest request)
        {
            var restaurantId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(restaurantId)) return Unauthorized();
            return Ok(await _orderService.UpdateOrderStatusAsync(restaurantId, id, request.Status));
        }
    }
}
