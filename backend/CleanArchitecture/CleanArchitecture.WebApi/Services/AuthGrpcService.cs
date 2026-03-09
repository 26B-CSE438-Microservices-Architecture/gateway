using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using CleanArchitecture.Core.Settings;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CleanArchitecture.WebApi.Grpc
{
    public class AuthGrpcService : AuthService.AuthServiceBase
    {
        private readonly JWTSettings _jwtSettings;

        public AuthGrpcService(IOptions<JWTSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public override Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
        {
            var response = new ValidateTokenResponse { IsValid = false };

            if (string.IsNullOrEmpty(request.Token))
                return Task.FromResult(response);

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

                tokenHandler.ValidateToken(request.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                response.IsValid = true;
                response.UserId = jwtToken.Claims.FirstOrDefault(x => x.Type == "uid")?.Value ?? string.Empty;
                
                var roles = jwtToken.Claims.Where(x => x.Type == "roles").Select(x => x.Value);
                response.Roles.AddRange(roles);
            }
            catch
            {
                response.IsValid = false;
            }

            return Task.FromResult(response);
        }
    }
}
