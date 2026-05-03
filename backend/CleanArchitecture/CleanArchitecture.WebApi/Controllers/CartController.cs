using CleanArchitecture.Core.DTOs.Order;
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
            return Ok(await _orderService.GetCartAsync(userId));
        }

        /// <summary>
        /// Adds a product to the user's basket.
        /// </summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddCartItemRequest request)
        {
            var userId = User.FindFirstValue("uid");
            try
            {
                return Ok(await _orderService.AddToCartAsync(userId, request));
            }
            catch (ApiException ex) when (ex.Message.Contains("PRODUCT_NOT_FOUND") || ex.Message.Contains("Product not found"))
            {
                // Attempt to fix synchronization: Fetch from Vendor Service and push to Order Service
                try 
                {
                    var product = await _vendorService.GetProductByIdAsync(request.ProductId);
                    if (product != null)
                    {
                        await _orderService.SyncProductAsync(new SyncProductRequest
                        {
                            ProductId = product.Id,
                            Name = product.Name,
                            Price = product.Price,
                            VendorId = "mock_vendor_id" // Ideally extracted from product or context
                        });

                        // Retry adding to cart
                        return Ok(await _orderService.AddToCartAsync(userId, request));
                    }
                }
                catch { /* If sync fails, fall through to original exception */ }

                throw; // Rethrow original exception if sync wasn't possible or failed
            }
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
