using CleanArchitecture.Infrastructure.Tests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.Infrastructure.Tests.Controllers
{
    public class AdminControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AdminControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllUsers_AsAdmin_ShouldReturn200()
        {
            // Arrange
            var token = await LoginAsAdminAsync();
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users?page=1&limit=10");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"total\":");
            content.Should().Contain("\"data\":");
        }

        [Fact]
        public async Task DeactivateAndActivateUser_ShouldWork()
        {
            // Arrange
            var token = await LoginAsAdminAsync();
            var targetUserId = await GetSampleUserIdAsync();

            // 1. Deactivate
            var deactRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/admin/users/{targetUserId}/deactivate");
            deactRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var deactResponse = await _client.SendAsync(deactRequest);
            deactResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 2. Activate
            var actRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/admin/users/{targetUserId}/activate");
            actRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var actResponse = await _client.SendAsync(actRequest);
            actResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetAllUsers_AsRegularUser_ShouldReturn403()
        {
            // Arrange
            var token = await RegisterAndLoginAsync("notadmin@example.com", "Password123!");
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // --- Helpers ---

        private async Task<string> LoginAsAdminAsync()
        {
            // sysadmin@trendyolgo.com is the correct seeded email (from DefaultSuperAdmin.cs)
            var loginRequest = new { email = "sysadmin@trendyolgo.com", password = "123Pa$$word!" };
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new System.Exception($"Admin Login Failed: {response.StatusCode} - {error}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            return json.RootElement.GetProperty("access_token").GetString()!;
        }

        private async Task<string> GetSampleUserIdAsync()
        {
            var email = $"admintest_{System.Guid.NewGuid():N}@example.com";
            await RegisterAndLoginAsync(email, "Password123!");
            
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Password123!" });
            var content = await loginResponse.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            return json.RootElement.GetProperty("user").GetProperty("id").GetString()!;
        }

        private async Task<string> RegisterAndLoginAsync(string email, string password)
        {
            await _client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                email,
                password,
                name = "Test",
                surname = "User"
            });
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("access_token").GetString()!;
        }
    }
}
