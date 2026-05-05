using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.DTOs.Vendor;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CleanArchitecture.Core.Exceptions;
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
        private readonly IVendorService _vendorService;

        public CartController(IOrderService orderService, IVendorService vendorService)
        {
            _orderService = orderService;
            _vendorService = vendorService;
        }

        /// <summary>
        /// Retrieves the current user's basket/cart.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            return Ok(await _orderService.GetCartAsync(userId));
        }

        /// <summary>
        /// Adds a product to the user's basket.
        /// Gateway adapts the request: fetches product from VendorService to resolve restaurantId,
        /// then sends the enriched request to Order Service in the expected format.
        /// </summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddCartItemRequest request)
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // 1. Ürünü çek, hangi restorana (VendorId) ait olduğunu bul
            ProductDto product;
            try
            {
                product = await _vendorService.GetProductByIdAsync(request.ProductId);
            }
            catch
            {
                return NotFound(new { error = "PRODUCT_NOT_FOUND", message = "Ürün bulunamadı." });
            }

            if (product == null)
            {
                return NotFound(new { error = "PRODUCT_NOT_FOUND", message = "Ürün bulunamadı." });
            }

            // 2. Order Service'in beklediği formatta istek oluştur
            var osRequest = new OrderServiceAddCartItemRequest
            {
                MenuItemId = product.Id,
                RestaurantId = product.VendorId,
                Quantity = request.Quantity
            };

            // 3. Gönder
            return Ok(await _orderService.AddToCartAsync(userId, osRequest));
        }

        /// <summary>
        /// Updates the quantity of an item in the basket.
        /// </summary>
        [HttpPut("items/{productId}")]
        public async Task<IActionResult> UpdateItem(string productId, [FromBody] UpdateCartItemRequest request)
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            return Ok(await _orderService.UpdateCartItemAsync(userId, productId, request));
        }

        /// <summary>
        /// Removes an item from the basket.
        /// </summary>
        [HttpDelete("items/{productId}")]
        public async Task<IActionResult> RemoveItem(string productId)
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _orderService.RemoveFromCartAsync(userId, productId);
            return NoContent();
        }

        /// <summary>
        /// Clears all items from the basket.
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

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
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
            
            var result = await _orderService.CheckoutAsync(userId, request, idempotencyKey);
            return StatusCode(201, result);
        }
    }
}
