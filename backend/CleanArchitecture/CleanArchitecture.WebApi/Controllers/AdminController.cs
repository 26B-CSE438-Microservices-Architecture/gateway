using CleanArchitecture.Core.DTOs.Admin;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/admin")]
    [ApiController]
    [Authorize(Roles = "SysAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>
        /// Lists all registered users with pagination. SysAdmin only.
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            return Ok(await _adminService.GetAllUsersAsync(page, limit));
        }

        /// <summary>
        /// Assigns a role to a specific user. SysAdmin only.
        /// </summary>
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> AssignRole(string id, [FromBody] AssignRoleRequest request)
        {
            return Ok(await _adminService.AssignRoleAsync(id, request));
        }
    }
}
