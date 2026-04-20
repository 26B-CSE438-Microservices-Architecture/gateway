using CleanArchitecture.Infrastructure.Tests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;

namespace CleanArchitecture.Infrastructure.Tests.Controllers
{
    public class InternalUsersIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public InternalUsersIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetUserById_ShouldReturn200()
        {
            // Arrange
            var userId = await GetSampleUserIdAsync();

            // Act
            var response = await _client.GetAsync($"/internal/v1/users/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain(userId);
        }

        [Fact]
        public async Task BulkLookup_ShouldReturnList()
        {
            // Arrange
            var userId = await GetSampleUserIdAsync();
            var request = new { userIds = new List<string> { userId, "non-existent" } };

            // Act
            var response = await _client.PostAsJsonAsync("/internal/v1/users/lookup", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"users\":");
            content.Should().Contain(userId);
        }

        [Fact]
        public async Task GetUserByEmail_ShouldReturnAuthInfo()
        {
            // Arrange
            var email = "internaltest@example.com";
            await RegisterAndLoginAsync(email, "Internal123!");

            // Act
            var response = await _client.GetAsync($"/internal/v1/users/by-email?email={email}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain(email);
            content.Should().Contain("hashed_password");
        }

        // --- Helpers ---

        private async Task<string> GetSampleUserIdAsync()
        {
            var email = $"idtest_{System.Guid.NewGuid():N}@example.com";
            await _client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                email,
                password = "Password123!",
                name = "ID",
                surname = "Test"
            });

            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Password123!" });
            var content = await loginResponse.Content.ReadAsStringAsync();
            
            // Debugging potential failures
            if (!loginResponse.IsSuccessStatusCode)
            {
                 throw new System.Exception($"Login for ID failed: {loginResponse.StatusCode} - {content}");
            }

            var json = JsonDocument.Parse(content);
            // In AuthenticationResponse, User is under "user" property, and Id might be capital or lower depending on serializer settings.
            // But per DTO definition, Id property is just Id (so "id" in JSON if lowercased).
            return json.RootElement.GetProperty("user").GetProperty("id").GetString()!;
        }

        private async Task RegisterAndLoginAsync(string email, string password)
        {
            await _client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                email,
                password,
                name = "Test",
                surname = "User"
            });
            await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        }
    }
}
