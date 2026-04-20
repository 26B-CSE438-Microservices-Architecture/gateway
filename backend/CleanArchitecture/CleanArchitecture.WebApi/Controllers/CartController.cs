using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/cart")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public CartController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Retrieves the current user's basket/cart.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _orderService.GetCartAsync(userId));
        }

        /// <summary>
        /// Adds a product to the user's basket.
        /// </summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddCartItemRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _orderService.AddToCartAsync(userId, request));
        }

        /// <summary>
        /// Updates the quantity of an item in the basket.
        /// </summary>
        [HttpPut("items/{productId}")]
        public async Task<IActionResult> UpdateItem(string productId, [FromBody] UpdateCartItemRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _orderService.UpdateCartItemAsync(userId, productId, request));
        }

        /// <summary>
        /// Removes an item from the basket.
        /// </summary>
        [HttpDelete("items/{productId}")]
        public async Task<IActionResult> RemoveItem(string productId)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _orderService.RemoveFromCartAsync(userId, productId));
        }

        /// <summary>
        /// Clears all items from the basket.
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var userId = User.FindFirstValue("uid");
            await _orderService.ClearCartAsync(userId);
            return NoContent();
        }

        /// <summary>
        /// Converts the current basket into a pending order. Requires Idempotency-Key.
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            var userId = User.FindFirstValue("uid");
            var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
            
            var result = await _orderService.CheckoutAsync(userId, request, idempotencyKey);
            return StatusCode(201, result);
        }
    }
}
