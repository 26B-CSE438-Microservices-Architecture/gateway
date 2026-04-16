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
                Issuer = "TestIssuer",
                Audience = "TestAudience",
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
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, "testuser"),
                new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
                new Claim("uid", userId)
            };

            foreach (var role in roles)
                claims.Add(new Claim("roles", role));

            var expires = expiredMinutesAgo > 0
                ? DateTime.UtcNow.AddMinutes(-expiredMinutesAgo)
                : DateTime.UtcNow.AddMinutes(_settings.DurationInMinutes);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static ServerCallContext CreateMockServerCallContext()
        {
            return new Mock<ServerCallContext>().Object;
        }
    }
}
