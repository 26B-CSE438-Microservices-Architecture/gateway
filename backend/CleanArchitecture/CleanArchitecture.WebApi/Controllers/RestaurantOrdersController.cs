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
            var restaurantId = User.FindFirstValue("restaurant_id");
            if (string.IsNullOrEmpty(restaurantId)) return BadRequest("User is not associated with any restaurant.");
            return Ok(await _orderService.GetRestaurantOrdersAsync(restaurantId, status, page, size));
        }

        /// <summary>
        /// Confirms a held order, triggering the payment capture process via SAGA.
        /// </summary>
        [HttpPatch("{id}/confirm")]
        public async Task<IActionResult> ConfirmOrder(string id)
        {
            var restaurantId = User.FindFirstValue("restaurant_id");
            if (string.IsNullOrEmpty(restaurantId)) return BadRequest("User is not associated with any restaurant.");
            
            var sagaService = HttpContext.RequestServices.GetService(typeof(IOrderSagaOrchestrator)) as IOrderSagaOrchestrator;
            var result = await sagaService.HandleRestaurantConfirmAsync(id, restaurantId);
            return Ok(new { message = "Order confirmed via SAGA", sagaStatus = result.Status });
        }

        /// <summary>
        /// Rejects a held order, triggering a payment hold release via SAGA.
        /// </summary>
        [HttpPatch("{id}/reject")]
        public async Task<IActionResult> RejectOrder(string id)
        {
            var restaurantId = User.FindFirstValue("restaurant_id");
            if (string.IsNullOrEmpty(restaurantId)) return BadRequest("User is not associated with any restaurant.");
            
            var sagaService = HttpContext.RequestServices.GetService(typeof(IOrderSagaOrchestrator)) as IOrderSagaOrchestrator;
            var result = await sagaService.HandleRestaurantRejectAsync(id, restaurantId, "restaurant_rejected");
            return Ok(new { message = "Order rejected via SAGA", sagaStatus = result.Status });
        }

        /// <summary>
        /// Manually updates the order status (e.g. PREPARING, DELIVERED).
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateOrderStatusRequest request)
        {
            var restaurantId = User.FindFirstValue("restaurant_id");
            if (string.IsNullOrEmpty(restaurantId)) return BadRequest("User is not associated with any restaurant.");
            return Ok(await _orderService.UpdateOrderStatusAsync(restaurantId, id, request.Status));
        }
    }
}
