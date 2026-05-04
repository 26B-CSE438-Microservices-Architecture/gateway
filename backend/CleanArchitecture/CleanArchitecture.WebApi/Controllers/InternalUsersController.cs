using CleanArchitecture.Core.DTOs.User;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("internal/v1/users")]
    [ApiController]
    // Internal endpoints are typicaly not authenticated via JWT but by cluster-level policies.
    public class InternalUsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private const string InternalSecretHeader = "X-Internal-Secret";

        public InternalUsersController(IUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
        }

        private bool IsInternalAuthorized()
        {
            if (!Request.Headers.TryGetValue(InternalSecretHeader, out var secret)) return false;
            var expectedSecret = _configuration["InternalSettings:Secret"];
            return secret == expectedSecret;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            if (!IsInternalAuthorized()) return Unauthorized(new { message = "Invalid Internal Secret" });
            var result = await _userService.GetInternalUserByIdAsync(userId);
            return Ok(result);
        }

        [HttpPost("lookup")]
        public async Task<IActionResult> BulkLookup([FromBody] UserLookupRequest request)
        {
            if (!IsInternalAuthorized()) return Unauthorized(new { message = "Invalid Internal Secret" });
            var users = await _userService.LookupUsersAsync(request.UserIds);
            return Ok(new { users = users });
        }

        [HttpGet("by-email")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
        {
            if (!IsInternalAuthorized()) return Unauthorized(new { message = "Invalid Internal Secret" });
            var result = await _userService.GetUserByEmailAsync(email);
            return Ok(result);
        }
    }

    public class UserLookupRequest
    {
        public List<string> UserIds { get; set; }
    }
}
