using System;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using CleanArchitecture.Core.Settings;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Text;
using System.Collections.Generic;

namespace CleanArchitecture.WebApi.Grpc
{
    public class AuthGrpcService : AuthService.AuthServiceBase
    {
        private readonly JWTSettings _jwtSettings;

        public AuthGrpcService(IOptions<JWTSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public override async Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
        {
            var response = new ValidateTokenResponse { IsValid = false };

            if (string.IsNullOrEmpty(request.Token))
                return response;

            try
            {
                var tokenHandler = new JsonWebTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                var result = await tokenHandler.ValidateTokenAsync(request.Token, validationParameters);

                if (result.IsValid)
                {
                    response.IsValid = true;
                    
                    // In JsonWebTokenHandler, claims are in result.ClaimsIdentity
                    var claims = result.ClaimsIdentity.Claims;
                    
                    response.UserId = claims.FirstOrDefault(x => x.Type == "uid")?.Value 
                                     ?? claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value 
                                     ?? string.Empty;
                    
                    var roles = claims.Where(x => x.Type == "roles" || x.Type == ClaimTypes.Role)
                                     .Select(x => x.Value)
                                     .Distinct();
                                     
                    response.Roles.AddRange(roles);
                }
            }
            catch
            {
                response.IsValid = false;
            }

            return response;
        }
    }
}
