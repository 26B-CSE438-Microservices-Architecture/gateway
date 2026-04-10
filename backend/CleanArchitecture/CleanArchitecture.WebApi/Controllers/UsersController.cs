using CleanArchitecture.Core.DTOs.User;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _userService.GetProfileAsync(userId));
        }

        [HttpPost("me/device")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
        {
            var userId = User.FindFirstValue("uid");
            await _userService.RegisterDeviceAsync(userId, request);
            return Ok(new { message = "Device registered successfully" });
        }

        [HttpGet("me/addresses")]
        public async Task<IActionResult> GetAddresses()
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _userService.GetAddressesAsync(userId));
        }

        [HttpPost("me/addresses")]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressRequest request)
        {
            var userId = User.FindFirstValue("uid");
            var address = await _userService.CreateAddressAsync(userId, request);
            return StatusCode(201, address);
        }

        [HttpPut("me/addresses/{id}")]
        public async Task<IActionResult> UpdateAddress(string id, [FromBody] UpdateAddressRequest request)
        {
            var userId = User.FindFirstValue("uid");
            var address = await _userService.UpdateAddressAsync(userId, id, request);
            return Ok(address);
        }

        [HttpDelete("me/addresses/{id}")]
        public async Task<IActionResult> DeleteAddress(string id)
        {
            var userId = User.FindFirstValue("uid");
            await _userService.DeleteAddressAsync(userId, id);
            return Ok(new { message = "Address deleted" });
        }

        [HttpGet("me/favorites")]
        public async Task<IActionResult> GetFavorites([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _userService.GetFavoritesAsync(userId, page, limit));
        }

        [HttpPost("me/favorites/{vendor_id}")]
        public async Task<IActionResult> AddFavorite(string vendor_id)
        {
            var userId = User.FindFirstValue("uid");
            await _userService.AddFavoriteAsync(userId, vendor_id);
            return Ok(new { message = "Vendor added to favorites" });
        }

        [HttpDelete("me/favorites/{vendor_id}")]
        public async Task<IActionResult> RemoveFavorite(string vendor_id)
        {
            var userId = User.FindFirstValue("uid");
            await _userService.RemoveFavoriteAsync(userId, vendor_id);
            return Ok(new { message = "Vendor removed from favorites" });
        }
    }
}
