using CleanArchitecture.Core.DTOs.User;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IVendorService _vendorService;

        public UsersController(IUserService userService, IVendorService vendorService)
        {
            _userService = userService;
            _vendorService = vendorService;
        }

        /// <summary>
        /// Get the authenticated user's own profile.
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _userService.GetProfileAsync(userId));
        }

        /// <summary>
        /// Update the authenticated user's own profile (name, phone only).
        /// </summary>
        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateProfileRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _userService.UpdateProfileAsync(userId, request));
        }

        /// <summary>
        /// List all saved addresses of the authenticated user.
        /// </summary>
        [Authorize]
        [HttpGet("me/addresses")]
        public async Task<IActionResult> GetAddresses()
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _userService.GetAddressesAsync(userId));
        }

        /// <summary>
        /// Add a new delivery address.
        /// </summary>
        [Authorize]
        [HttpPost("me/addresses")]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressRequest request)
        {
            var userId = User.FindFirstValue("uid");
            var address = await _userService.CreateAddressAsync(userId, request);
            return StatusCode(201, address);
        }

        /// <summary>
        /// Remove a delivery address.
        /// </summary>
        [Authorize]
        [HttpDelete("me/addresses/{id}")]
        public async Task<IActionResult> DeleteAddress(string id)
        {
            var userId = User.FindFirstValue("uid");
            await _userService.DeleteAddressAsync(userId, id);
            return NoContent();
        }

        // --- Favorites Aggregation (BFF Logic) ---

        /// <summary>
        /// Get favorites with details aggregated from User and Restaurant services.
        /// </summary>
        [Authorize]
        [HttpGet("me/favorites")]
        public async Task<IActionResult> GetFavorites([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            var userId = User.FindFirstValue("uid");

            // 1. Fetch favorite IDs from User Service (Inner Contract)
            var userFavorites = await _userService.GetFavoritesAsync(userId, page, limit);

            // 2. Fetch details for these IDs from Restaurant Service (Bulk Lookup)
            var vendorIds = userFavorites.Data.Select(f => f.VendorId).ToList();
            var vendorDetails = await _vendorService.LookupVendorsAsync(vendorIds);

            // 3. Compose (Merge) data for Mobile Contract
            // If restaurant doesn't return a vendor (inactive/deleted), skip it in final list
            var aggregatedData = userFavorites.Data
                .Select(f => {
                    var detail = vendorDetails.FirstOrDefault(v => v.VendorId == f.VendorId);
                    return detail != null ? new BffFavoriteDto {
                        VendorId = f.VendorId,
                        Name = detail.Name,
                        ImageUrl = detail.ImageUrl
                    } : null;
                })
                .Where(x => x != null)
                .ToList();

            // Total stays as User Service total (source-of-truth for relationships)
            return Ok(new PagedFavoritesResponse<BffFavoriteDto>
            {
                Page = userFavorites.Page,
                Limit = userFavorites.Limit,
                Total = userFavorites.Total,
                Data = aggregatedData
            });
        }

        /// <summary>
        /// Add a vendor to favorites.
        /// </summary>
        [Authorize]
        [HttpPost("me/favorites/{vendor_id}")]
        public async Task<IActionResult> AddFavorite(string vendor_id)
        {
            var userId = User.FindFirstValue("uid");
            await _userService.AddFavoriteAsync(userId, vendor_id);
            return StatusCode(201, new { message = "Favorite added" });
        }

        /// <summary>
        /// Remove a vendor from favorites.
        /// </summary>
        [Authorize]
        [HttpDelete("me/favorites/{vendor_id}")]
        public async Task<IActionResult> RemoveFavorite(string vendor_id)
        {
            var userId = User.FindFirstValue("uid");
            await _userService.RemoveFavoriteAsync(userId, vendor_id);
            return NoContent();
        }
    }
}
