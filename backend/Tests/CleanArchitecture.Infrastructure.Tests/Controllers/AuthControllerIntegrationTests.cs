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
    public class AuthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // ========================
        // REGISTER TESTS
        // ========================

        [Fact]
        public async Task Register_WithValidData_ShouldReturn200()
        {
            // Arrange
            var request = new
            {
                email = "integrationtest@example.com",
                password = "IntTest123!",
                name = "Integration",
                surname = "Test",
                phone_number = "5551234567"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("registered successfully");
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ShouldReturn400()
        {
            // Arrange — first register
            var request = new
            {
                email = "duplicate@example.com",
                password = "IntTest123!",
                name = "Dup",
                surname = "User"
            };
            await _client.PostAsJsonAsync("/api/v1/auth/register", request);

            // Act — second register with same email
            var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ========================
        // LOGIN TESTS
        // ========================

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange — register first
            var registerRequest = new
            {
                email = "logintest@example.com",
                password = "LoginTest123!",
                name = "Login",
                surname = "Test"
            };
            await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

            var loginRequest = new
            {
                email = "logintest@example.com",
                password = "LoginTest123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new System.Exception("SERVER RETURNED: " + response.StatusCode + " BODY: " + content);
            }
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().Contain("access_token");
            content.Should().Contain("refresh_token");
        }

        [Fact]
        public async Task Login_WithWrongPassword_ShouldReturn400()
        {
            // Arrange
            var registerRequest = new
            {
                email = "wrongpass@example.com",
                password = "CorrectPass123!",
                name = "Wrong",
                surname = "Pass"
            };
            await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

            var loginRequest = new
            {
                email = "wrongpass@example.com",
                password = "TotallyWrongPassword!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ========================
        // PROTECTED ENDPOINT TESTS
        // ========================

        [Fact]
        public async Task GetMe_WithoutToken_ShouldReturn401()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMe_WithValidToken_ShouldReturnUserInfo()
        {
            // Arrange — register and login to get token
            var token = await RegisterAndLoginAsync("metest@example.com", "MeTest123!");

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("metest@example.com");
        }

        // ========================
        // VERIFY TOKEN TESTS
        // ========================

        [Fact]
        public async Task VerifyToken_WithValidToken_ShouldReturnValid()
        {
            // Arrange
            var token = await RegisterAndLoginAsync("verifytest@example.com", "VerifyTest123!");

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/verify-token", new { token });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"is_valid\":true");
        }

        [Fact]
        public async Task VerifyToken_WithInvalidToken_ShouldReturnInvalid()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/verify-token", 
                new { token = "invalid.jwt.token" });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"is_valid\":false");
        }

        // ========================
        // HEALTH CHECK TEST
        // ========================

        [Fact]
        public async Task HealthEndpoint_ShouldReturn200()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // ========================
        // REFRESH TOKEN TEST
        // ========================

        [Fact]
        public async Task RefreshToken_WithValidToken_ShouldReturnNewAccessToken()
        {
            // Arrange — register, login, and get refresh token
            var email = "refreshtest@example.com";
            var password = "RefreshTest123!";

            await _client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                email,
                password,
                name = "Refresh",
                surname = "Test"
            });

            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var loginJson = JsonDocument.Parse(loginContent);
            var refreshToken = loginJson.RootElement.GetProperty("refresh_token").GetString();

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", 
                new { refresh_token = refreshToken });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("access_token");
        }

        // ========================
        // HELPERS
        // ========================

        private async Task<string> RegisterAndLoginAsync(string email, string password)
        {
            await _client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                email,
                password,
                name = "Test",
                surname = "User"
            });

            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var loginJson = JsonDocument.Parse(loginContent);
            return loginJson.RootElement.GetProperty("access_token").GetString()!;
        }
    }
}
