using CleanArchitecture.Core.DTOs.Account;
using CleanArchitecture.Core.DTOs.Email;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Settings;
using CleanArchitecture.Core.Wrappers;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Helpers;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly JWTSettings _jwtSettings;
        private readonly IDateTimeService _dateTimeService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserService _userService;

        public AccountService(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<JWTSettings> jwtSettings,
            IDateTimeService dateTimeService,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            ApplicationDbContext dbContext,
            IUserService userService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtSettings = jwtSettings.Value;
            _dateTimeService = dateTimeService;
            _signInManager = signInManager;
            this._emailService = emailService;
            _dbContext = dbContext;
            _userService = userService;
        }

        public async Task<AuthenticationResponse> LoginAsync(AuthenticationRequest request, string ipAddress)
        {
            var user = await _userManager.Users.Include(u => u.RefreshTokens).SingleOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                throw new ApiException($"No Accounts Registered with {request.Email}.");
            }
            var result = await _signInManager.PasswordSignInAsync(user.UserName, request.Password, false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                throw new ApiException($"Invalid Credentials for '{request.Email}'.");
            }
            if (!user.EmailConfirmed)
            {
                throw new ApiException($"Account Not Confirmed for '{request.Email}'.");
            }
            JwtSecurityToken jwtSecurityToken = await GenerateJWToken(user);
            AuthenticationResponse response = new AuthenticationResponse();
            response.JWToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            response.ExpiresIn = 3600;
            response.User = new UserDto
            {
                Id = user.Id,
                Name = user.FirstName,
                Surname = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };
            
            var refreshToken = GenerateRefreshToken(ipAddress);
            if (user.RefreshTokens == null)
                user.RefreshTokens = new List<RefreshToken>();
                
            user.RefreshTokens.Add(refreshToken);
            await _userManager.UpdateAsync(user);

            response.RefreshToken = refreshToken.Token;
            return response;
        }

        public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress)
        {
            var user = await _userManager.Users.Include(u => u.RefreshTokens).SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == request.RefreshToken));
            
            if (user == null)
            {
                throw new ApiException("Invalid or expired refresh token.");
            }

            var refreshToken = user.RefreshTokens.Single(x => x.Token == request.RefreshToken);

            if (!refreshToken.IsActive)
                 throw new ApiException("Token is no longer active");

            // Revoke old token
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;

            // Generate new ones
            var newRefreshToken = GenerateRefreshToken(ipAddress);
            user.RefreshTokens.Add(newRefreshToken);
            await _userManager.UpdateAsync(user);

            JwtSecurityToken jwtSecurityToken = await GenerateJWToken(user);
            
            return new RefreshTokenResponse
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                ExpiresIn = 3600
            };
        }

        public async Task<bool> LogoutAsync(LogoutRequest request, string ipAddress)
        {
            var user = await _userManager.Users.Include(u => u.RefreshTokens).SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == request.RefreshToken));
            
            if (user == null) return false;

            var refreshToken = user.RefreshTokens.Single(x => x.Token == request.RefreshToken);

            if (!refreshToken.IsActive)
                 throw new ApiException("Token is no longer active");

            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;

            await _userManager.UpdateAsync(user);
            return true;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, string origin)
        {
            var userWithSameEmail = await _userManager.FindByEmailAsync(request.Email);
            if (userWithSameEmail != null)
            {
                throw new ApiException($"Email {request.Email} is already registered.");
            }

            // 1. Create the user in the User Service (Source of Truth)
            var externalId = await _userService.RegisterUserInServiceAsync(request);

            // 2. Create the user in the Gateway's local Identity database
            var user = new ApplicationUser
            {
                Id = externalId, // Sync IDs!
                Email = request.Email,
                FirstName = request.Name,
                LastName = request.Surname,
                UserName = request.Email,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true
            };
            
            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, Roles.Customer.ToString());
                return new RegisterResponse
                {
                    Message = "User registered successfully",
                    UserId = user.Id
                };
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ApiException($"{errors}");
            }
        }

        private async Task<JwtSecurityToken> GenerateJWToken(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var claimsList = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("uid", user.Id),
                new Claim("ip", IpHelper.GetIpAddress())
            };

            claimsList.AddRange(userClaims);
            foreach (var role in roles)
            {
                claimsList.Add(new Claim("roles", role));
            }

            var jwtKeyStr = _jwtSettings.Key ?? _jwtSettings.Secret ?? "Default_Secure_Key_For_Gateway_Service_2026_!@#";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKeyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claimsList),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor) as JwtSecurityToken;
            
            return token;
        }

        private string RandomTokenString()
        {
            var randomNumber = new byte[32];
            RandomNumberGenerator.Fill(randomNumber);
            // convert random bytes to hex string
            return BitConverter.ToString(randomNumber).Replace("-", "");
        }

        private async Task<string> SendVerificationEmail(ApplicationUser user, string origin)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var route = "api/account/confirm-email/";
            var _enpointUri = new Uri(string.Concat($"{origin}/", route));
            var verificationUri = QueryHelpers.AddQueryString(_enpointUri.ToString(), "userId", user.Id);
            verificationUri = QueryHelpers.AddQueryString(verificationUri, "code", code);
            //Email Service Call Here
            return verificationUri;
        }

        public async Task<string> ConfirmEmailAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                return  $"Account Confirmed for {user.Email}. You can now use the /api/Account/authenticate endpoint.";
            }
            else
            {
                throw new ApiException($"An error occured while confirming {user.Email}.");
            }
        }

        private RefreshToken GenerateRefreshToken(string ipAddress)
        {
            return new RefreshToken
            {
                Token = RandomTokenString(),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }

        public async Task<EmailRequest> ForgotPassword(ForgotPasswordRequest model, string origin)
        {
            var account = await _userManager.FindByEmailAsync(model.Email);

            // always return ok response to prevent email enumeration
            if (account == null) throw new ApiException("User not found");

            var code = await _userManager.GeneratePasswordResetTokenAsync(account);
            var route = "api/account/reset-password/";
            var _enpointUri = new Uri(string.Concat($"{origin}/", route));
            var emailRequest = new EmailRequest()
            {
                Body = $"You reset token is - {code}",
                To = model.Email,
                Subject = "Reset Password",
            };
            //TODO: Attach Email Service here and configure it via appsettings
            //await _emailService.SendAsync(emailRequest);
            return emailRequest;
        }

        public async Task<string> ResetPassword(ResetPasswordRequest model)
        {
            var account = await _userManager.FindByEmailAsync(model.Email);
            if (account == null) throw new ApiException($"No Accounts Registered with {model.Email}.");
            var result = await _userManager.ResetPasswordAsync(account, model.Token, model.Password);
            if (result.Succeeded)
            {
                return  $"Password Resetted.";
            }
            else
            {
                throw new ApiException($"Error occured while reseting the password.");
            }
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new ApiException("User not found.");

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ApiException(errors);
            }
            return true;
        }

        public async Task<bool> UpdateProfileAsync(string userId, UpdateProfileRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new ApiException("User not found.");

            user.FirstName = request.Name ?? user.FirstName;
            user.LastName = request.Surname ?? user.LastName;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> DeleteAccountAsync(string userId, DeleteAccountRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new ApiException("User not found.");

            var checkPassword = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!checkPassword) throw new ApiException("Invalid password.");

            // Directly delete related RefreshTokens using DbContext to ensure they are gone
            // Using ExecuteDeleteAsync for better performance and to bypass navigation property issues
            await _dbContext.Set<RefreshToken>().Where(x => EF.Property<string>(x, "ApplicationUserId") == userId).ExecuteDeleteAsync();

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }
    }
}
