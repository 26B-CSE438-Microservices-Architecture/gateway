using CleanArchitecture.Core.DTOs.Account;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
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

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync(AuthenticationRequest request)
        {
            return Ok(await _accountService.LoginAsync(request, GenerateIPAddress()));
        }

        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync(RegisterRequest request)
        {
            var origin = Request.Headers["origin"];
            return Ok(await _accountService.RegisterAsync(request, origin));
        }

        /// <summary>
        /// Refreshes an expired JWT token using a valid refresh token.
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshAsync(RefreshTokenRequest request)
        {
            return Ok(await _accountService.RefreshTokenAsync(request, GenerateIPAddress()));
        }

        /// <summary>
        /// Revokes the user's current session and invalidates the refresh token.
        /// </summary>
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

        /// <summary>
        /// Changes the password for the authenticated user.
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            if (string.IsNullOrEmpty(request?.OldPassword) || string.IsNullOrEmpty(request?.NewPassword))
            {
                return BadRequest(new { error = "BAD_REQUEST", message = "old_password and new_password are required." });
            }

            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            return Ok(await _accountService.ChangePasswordAsync(userId, request));
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _accountService.UpdateProfileAsync(userId, request));
        }

        /// <summary>
        /// Permanently deletes the user account.
        /// </summary>
        [Authorize]
        [HttpDelete("account")]
        public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _accountService.DeleteAccountAsync(userId, request));
        }

        /// <summary>
        /// Confirms the user's email address using a verification code.
        /// </summary>
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmailAsync([FromQuery] string userId, [FromQuery] string code)
        {
            var origin = Request.Headers["origin"].FirstOrDefault() ?? "https://localhost:9001";
            return Ok(await _accountService.ConfirmEmailAsync(userId, code));
        }

        /// <summary>
        /// Sends a password reset email to the specified address.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest model)
        {
            var origin = Request.Headers["origin"].FirstOrDefault() ?? "https://localhost:9001";
            var result = await _accountService.ForgotPassword(model, origin);
            return Ok(result);
        }

        /// <summary>
        /// Resets the user's password using a reset token.
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
        {
            return Ok(await _accountService.ResetPassword(model));
        }

        /// <summary>
        /// Returns the current authenticated user's profile extracted from the JWT token.
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue("uid");
            var email = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email);
            var roles = User.FindAll("roles").Select(c => c.Value).ToList();

            return Ok(new
            {
                id = userId,
                email = email,
                roles = roles
            });
        }

        /// <summary>
        /// Validates a JWT token via REST. Returns validity status, user id and roles.
        /// </summary>
        [HttpPost("verify-token")]
        public async Task<IActionResult> VerifyToken([FromBody] VerifyTokenRequest request)
        {
            if (string.IsNullOrEmpty(request?.Token))
                return BadRequest(new { error = "INVALID_REQUEST", message = "Token is required." });

            try
            {
                var jwtSettings = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<CleanArchitecture.Core.Settings.JWTSettings>>().Value;
                var tokenHandler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
                var key = System.Text.Encoding.UTF8.GetBytes(jwtSettings.Key);

                var validationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = System.TimeSpan.FromMinutes(5)
                };

                var result = await tokenHandler.ValidateTokenAsync(request.Token, validationParameters);

                if (result.IsValid)
                {
                    var claims = result.ClaimsIdentity.Claims;
                    var uid = claims.FirstOrDefault(x => x.Type == "uid")?.Value;
                    var roles = claims.Where(x => x.Type == "roles" || x.Type == ClaimTypes.Role)
                                     .Select(x => x.Value)
                                     .Distinct()
                                     .ToList();

                    return Ok(new
                    {
                        is_valid = true,
                        user_id = uid,
                        roles = roles
                    });
                }
                
                return Ok(new { is_valid = false, user_id = (string)null, roles = new List<string>() });
            }
            catch
            {
                return Ok(new
                {
                    is_valid = false,
                    user_id = (string)null,
                    roles = new List<string>()
                });
            }
        }

        private string GenerateIPAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
        }
    }
}