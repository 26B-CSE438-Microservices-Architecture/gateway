using CleanArchitecture.Core.DTOs.Account;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAccountService _accountService;
        public AuthController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync(AuthenticationRequest request)
        {
            return Ok(await _accountService.LoginAsync(request, GenerateIPAddress()));
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync(RegisterRequest request)
        {
            var origin = Request.Headers["origin"];
            return Ok(await _accountService.RegisterAsync(request, origin));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshAsync(RefreshTokenRequest request)
        {
            return Ok(await _accountService.RefreshTokenAsync(request, GenerateIPAddress()));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> LogoutAsync(LogoutRequest request)
        {
            var result = await _accountService.LogoutAsync(request, GenerateIPAddress());
            if (result)
            {
                return Ok(new { message = "Logged out successfully" });
            }
            return BadRequest(new { message = "Invalid token" });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _accountService.ChangePasswordAsync(userId, request));
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _accountService.UpdateProfileAsync(userId, request));
        }

        [Authorize]
        [HttpDelete("account")]
        public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _accountService.DeleteAccountAsync(userId, request));
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmailAsync([FromQuery] string userId, [FromQuery] string code)
        {
            var origin = Request.Headers["origin"].FirstOrDefault() ?? "https://localhost:9001";
            return Ok(await _accountService.ConfirmEmailAsync(userId, code));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest model)
        {
            var origin = Request.Headers["origin"].FirstOrDefault() ?? "https://localhost:9001";
            var result = await _accountService.ForgotPassword(model, origin);
            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
        {
            return Ok(await _accountService.ResetPassword(model));
        }

        private string GenerateIPAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
    }
}