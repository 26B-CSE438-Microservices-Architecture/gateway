using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/orders/restaurant")]
    [ApiController]
    [Authorize(Roles = "SysAdmin,RestaurantAdmin")]
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
            // For mock, restaurantId is hardcoded
            return Ok(await _orderService.GetRestaurantOrdersAsync("mock_restaurant_1", status, page, size));
        }

        /// <summary>
        /// Confirms a held order, triggering the payment capture process.
        /// </summary>
        [HttpPatch("{id}/confirm")]
        public async Task<IActionResult> ConfirmOrder(string id)
        {
            return Ok(await _orderService.ConfirmOrderAsync("mock_restaurant_1", id));
        }

        /// <summary>
        /// Rejects a held order, triggering a payment hold release.
        /// </summary>
        [HttpPatch("{id}/reject")]
        public async Task<IActionResult> RejectOrder(string id)
        {
            return Ok(await _orderService.RejectOrderAsync("mock_restaurant_1", id));
        }

        /// <summary>
        /// Manually updates the order status (e.g. PREPARING, DELIVERED).
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateOrderStatusRequest request)
        {
            return Ok(await _orderService.UpdateOrderStatusAsync("mock_restaurant_1", id, request.Status));
        }
    }
}
