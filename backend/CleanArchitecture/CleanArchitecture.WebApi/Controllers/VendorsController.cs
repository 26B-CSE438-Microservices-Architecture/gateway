using CleanArchitecture.Core.DTOs.Vendor;
using CleanArchitecture.Core.DTOs.Review;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/vendors")]
    [ApiController]
    public class VendorsController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly IReviewService _reviewService;
        private readonly ICampaignService _campaignService;

        public VendorsController(
            IVendorService vendorService,
            IReviewService reviewService,
            ICampaignService campaignService)
        {
            _vendorService = vendorService;
            _reviewService = reviewService;
            _campaignService = campaignService;
        }

        /// <summary>
        /// Retrieves a paginated list of all restaurants/vendors.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetVendors([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            return Ok(await _vendorService.GetVendorsAsync(page, limit));
        }

        /// <summary>
        /// Retrieves detailed profile and information for a specific restaurant.
        /// </summary>
        [HttpGet("{vendor_id}")]
        public async Task<IActionResult> GetVendor(string vendor_id)
        {
            return Ok(await _vendorService.GetVendorByIdAsync(vendor_id));
        }

        /// <summary>
        /// Lists restaurants within a specific geographic radius.
        /// </summary>
        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearbyVendors([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radius = 5.0)
        {
            return Ok(await _vendorService.GetNearbyVendorsAsync(lat, lng, radius));
        }

        /// <summary>
        /// Retrieves the full menu structure (sections and products) for a restaurant.
        /// </summary>
        [HttpGet("{vendor_id}/menu")]
        public async Task<IActionResult> GetVendorMenu(string vendor_id)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(vendor_id);
            return Ok(vendor.MenuSections);
        }

        /// <summary>
        /// Registers a new restaurant in the system.
        /// </summary>
        [Authorize(Roles = "Admin,RestaurantOwner,restaurant_owner")]
        [HttpPost]
        public async Task<IActionResult> CreateVendor([FromBody] CreateVendorDto request)
        {
            var userId = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(request.OwnerId) && !string.IsNullOrEmpty(userId))
            {
                request.OwnerId = userId.ToLower();
            }

            var id = await _vendorService.CreateVendorAsync(request);
            return CreatedAtAction(nameof(GetVendor), new { vendor_id = id }, new { id });
        }

        /// <summary>
        /// Updates the core profile details of a restaurant.
        /// </summary>
        [Authorize(Roles = "Admin,RestaurantOwner,restaurant_owner")]
        [HttpPut("{vendor_id}")]
        public async Task<IActionResult> UpdateVendor(string vendor_id, [FromBody] UpdateVendorDto request)
        {
            return Ok(await _vendorService.UpdateVendorAsync(vendor_id, request));
        }

        /// <summary>
        /// Updates the operational status (Active/Inactive) of a restaurant.
        /// </summary>
        [Authorize(Roles = "Admin,RestaurantOwner,restaurant_owner")]
        [HttpPatch("{vendor_id}/status")]
        public async Task<IActionResult> UpdateVendorStatus(string vendor_id, [FromBody] UpdateStatusDto request)
        {
            return Ok(await _vendorService.UpdateVendorStatusAsync(vendor_id, request));
        }

        [Authorize(Roles = "Admin,RestaurantOwner,restaurant_owner")]
        [HttpDelete("{vendor_id}")]
        public async Task<IActionResult> DeleteVendor(string vendor_id)
        {
            return Ok(await _vendorService.DeleteVendorAsync(vendor_id));
        }

        [Authorize(Roles = "Admin,RestaurantOwner,restaurant_owner")]
        [HttpPost("{vendor_id}/categories")]
        public async Task<IActionResult> CreateCategory(string vendor_id, [FromBody] CreateCategoryDto request)
        {
            var id = await _vendorService.CreateCategoryAsync(vendor_id, request);
            return Ok(new { id });
        }

        [Authorize(Roles = "Admin,RestaurantOwner,restaurant_owner")]
        [HttpPut("/api/v1/categories/{id}")]
        public async Task<IActionResult> UpdateCategory(string id, [FromBody] UpdateCategoryDto request)
        {
            return Ok(await _vendorService.UpdateCategoryAsync(id, request));
        }

        [Authorize(Roles = "Admin,RestaurantOwner,restaurant_owner")]
        [HttpDelete("/api/v1/categories/{id}")]
        public async Task<IActionResult> DeleteCategory(string id)
        {
            return Ok(await _vendorService.DeleteCategoryAsync(id));
        }

        [Authorize(Roles = "Admin,RestaurantOwner,restaurant_owner")]
        [HttpPost("/api/v1/categories/{categoryId}/products")]
        public async Task<IActionResult> CreateProduct(string categoryId, [FromBody] CreateProductDto request)
        {
            var id = await _vendorService.CreateProductAsync(categoryId, request);
            return Ok(new { id });
        }

        [Authorize(Roles = "Admin,RestaurantOwner,restaurant_owner")]
        [HttpPut("/api/v1/products/{id}")]
        public async Task<IActionResult> UpdateProduct(string id, [FromBody] UpdateProductDto request)
        {
            return Ok(await _vendorService.UpdateProductAsync(id, request));
        }

        [Authorize(Roles = "Admin,RestaurantOwner,restaurant_owner")]
        [HttpPatch("/api/v1/products/{id}/stock")]
        public async Task<IActionResult> UpdateProductStock(string id, [FromBody] UpdateStockDto request)
        {
            return Ok(await _vendorService.UpdateProductStockAsync(id, request));
        }

        [Authorize(Roles = "Admin,RestaurantOwner,restaurant_owner")]
        [HttpDelete("/api/v1/products/{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            return Ok(await _vendorService.DeleteProductAsync(id));
        }

        /// <summary>
        /// Retrieves customer reviews and ratings for a restaurant.
        /// </summary>
        [HttpGet("{vendor_id}/reviews")]
        public async Task<IActionResult> GetVendorReviews(
            string vendor_id,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            return Ok(await _reviewService.GetVendorReviewsAsync(vendor_id, page, limit));
        }

        /// <summary>
        /// Retrieves active promotions and campaigns for a restaurant.
        /// </summary>
        [HttpGet("{vendor_id}/campaigns")]
        public async Task<IActionResult> GetVendorCampaigns(
            string vendor_id,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            return Ok(await _campaignService.GetVendorCampaignsAsync(vendor_id, page, limit));
        }
    }
}
