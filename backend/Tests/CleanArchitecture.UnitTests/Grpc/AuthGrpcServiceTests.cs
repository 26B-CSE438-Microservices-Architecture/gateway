using CleanArchitecture.Core.Settings;
using CleanArchitecture.WebApi.Grpc;
using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.UnitTests.Grpc
{
    public class AuthGrpcServiceTests
    {
        private readonly JWTSettings _settings;
        private readonly AuthGrpcService _grpcService;

        public AuthGrpcServiceTests()
        {
            _settings = new JWTSettings
            {
                Key = "5b1b4b12330f40d8924b22c7302409df5b1b4b12330f40d8924b22c7302409df",
                Issuer = "Gateway.Auth.Service",
                Audience = "Gateway.Clients",
                DurationInMinutes = 60
            };

            _grpcService = new AuthGrpcService(Options.Create(_settings));
        }

        [Fact]
        public async Task ValidateToken_WithValidToken_ShouldReturnValid()
        {
            // Arrange
            var token = GenerateTestToken("user-abc", new[] { "Customer" });
            var request = new ValidateTokenRequest { Token = token };
            var context = CreateMockServerCallContext();

            // Act
            var response = await _grpcService.ValidateToken(request, context);

            // Assert
            response.IsValid.Should().BeTrue();
            response.UserId.Should().Be("user-abc");
            response.Roles.Should().Contain("Customer");
        }

        [Fact]
        public async Task ValidateToken_WithExpiredToken_ShouldReturnInvalid()
        {
            // Arrange — token expired 1 hour ago
            var token = GenerateTestToken("user-abc", new[] { "Customer" }, expiredMinutesAgo: 60);
            var request = new ValidateTokenRequest { Token = token };
            var context = CreateMockServerCallContext();

            // Act
            var response = await _grpcService.ValidateToken(request, context);

            // Assert
            response.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateToken_WithEmptyToken_ShouldReturnInvalid()
        {
            // Arrange
            var request = new ValidateTokenRequest { Token = "" };
            var context = CreateMockServerCallContext();

            // Act
            var response = await _grpcService.ValidateToken(request, context);

            // Assert
            response.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateToken_WithGarbageToken_ShouldReturnInvalid()
        {
            // Arrange
            var request = new ValidateTokenRequest { Token = "this.is.not.a.jwt.token" };
            var context = CreateMockServerCallContext();

            // Act
            var response = await _grpcService.ValidateToken(request, context);

            // Assert
            response.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateToken_WithMultipleRoles_ShouldReturnAllRoles()
        {
            // Arrange
            var token = GenerateTestToken("admin-user", new[] { "SysAdmin", "Customer" });
            var request = new ValidateTokenRequest { Token = token };
            var context = CreateMockServerCallContext();

            // Act
            var response = await _grpcService.ValidateToken(request, context);

            // Assert
            response.IsValid.Should().BeTrue();
            response.UserId.Should().Be("admin-user");
            response.Roles.Should().HaveCount(2);
            response.Roles.Should().Contain("SysAdmin");
            response.Roles.Should().Contain("Customer");
        }

        // ========================
        // HELPERS
        // ========================

        private string GenerateTestToken(string userId, string[] roles, int expiredMinutesAgo = 0)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_settings.Key);
            
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, "testuser"),
                new Claim("uid", userId)
            };

            foreach (var role in roles)
                claims.Add(new Claim("roles", role));

            DateTime now = DateTime.UtcNow;
            DateTime expires;
            DateTime? notBefore = null;

            if (expiredMinutesAgo > 0)
            {
                expires = now.AddMinutes(-expiredMinutesAgo);
                notBefore = expires.AddMinutes(-60); // Ensure notBefore is earlier than expires
            }
            else
            {
                expires = now.AddMinutes(_settings.DurationInMinutes);
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                NotBefore = notBefore,
                Issuer = _settings.Issuer,
                Audience = _settings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static ServerCallContext CreateMockServerCallContext()
        {
            return new Mock<ServerCallContext>().Object;
        }
    }
}
