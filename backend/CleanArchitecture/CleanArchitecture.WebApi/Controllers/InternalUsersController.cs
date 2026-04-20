using CleanArchitecture.Core.DTOs.User;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("internal/v1/users")]
    [ApiController]
    // Internal endpoints are typicaly not authenticated via JWT but by cluster-level policies.
    // For mock purposes, we allow anonymous access to simulate service-to-service calls.
    public class InternalUsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public InternalUsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Fetch basic user profile by ID. Used by Order and Payment services.
        /// </summary>
        /// <summary>
        /// Retrieves non-sensitive internal user data (e.g. for Order Service).
        /// Requires X-Internal-Secret header.
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var result = await _userService.GetInternalUserByIdAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Bulk fetch multiple users by a list of IDs.
        /// </summary>
        [HttpPost("lookup")]
        public async Task<IActionResult> BulkLookup([FromBody] UserLookupRequest request)
        {
            var users = await _userService.LookupUsersAsync(request.UserIds);
            return Ok(new { users = users });
        }

        /// <summary>
        /// Look up a user by email address. Used by Gateway/Auth for credential verification.
        /// </summary>
        [HttpGet("by-email")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
        {
            var result = await _userService.GetUserByEmailAsync(email);
            return Ok(result);
        }
    }

    public class UserLookupRequest
    {
        public List<string> UserIds { get; set; }
    }
}
