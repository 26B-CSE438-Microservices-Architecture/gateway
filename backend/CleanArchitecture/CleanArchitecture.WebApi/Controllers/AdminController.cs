using CleanArchitecture.Core.DTOs.Admin;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/admin")]
    [ApiController]
    [Authorize(Roles = "SysAdmin")] // Aligning with system roles
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;

        public AdminController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Retrieves a paginated list of all users in the system.
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            return Ok(await _userService.GetAllUsersAdminAsync(page, limit));
        }

        /// <summary>
        /// Retrieves detailed information for any user by their ID.
        /// </summary>
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            return Ok(await _userService.GetInternalUserByIdAsync(userId));
        }

        /// <summary>
        /// Soft delete — marks user as deactivated.
        /// </summary>
        [HttpPatch("users/{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            await _userService.DeactivateUserAsync(id);
            return Ok(new { message = "User deactivated successfully" });
        }

        /// <summary>
        /// Re-activate a user account.
        /// </summary>
        [HttpPatch("users/{id}/activate")]
        public async Task<IActionResult> ActivateUser(string id)
        {
            await _userService.ActivateUserAsync(id);
            return Ok(new { message = "User activated successfully" });
        }
    }
}
