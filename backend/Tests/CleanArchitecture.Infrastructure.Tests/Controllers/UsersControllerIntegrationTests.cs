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
    public class UsersControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public UsersControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetProfile_WithValidToken_ShouldReturn200AndProfile()
        {
            // Arrange
            var token = await RegisterAndLoginAsync("profiletest@example.com", "Profile123!");
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("profiletest@example.com");
            content.Should().Contain("\"addresses\":");
        }

        [Fact]
        public async Task UpdateProfile_WithValidData_ShouldUpdateInfo()
        {
            // Arrange
            var token = await RegisterAndLoginAsync("updatetest@example.com", "Update123!");
            var updateRequest = new { name = "New Name", phone = "9998887766" };

            var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/users/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(updateRequest);

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("New Name");
            content.Should().Contain("9998887766");
        }

        [Fact]
        public async Task AddressBook_FullCycle_ShouldWork()
        {
            // Arrange
            var token = await RegisterAndLoginAsync("addrtest@example.com", "Addr123!");
            var newAddr = new { label = "Work", city = "Ankara", street = "Cankaya", postalCode = "06000", lat = 39.9, lng = 32.8 };

            // 1. Create Address
            var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/addresses");
            createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            createRequest.Content = JsonContent.Create(newAddr);
            var createResponse = await _client.SendAsync(createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var addrId = JsonDocument.Parse(createContent).RootElement.GetProperty("id").GetString();

            // 2. Get Addresses
            var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me/addresses");
            getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var getResponse = await _client.SendAsync(getRequest);
            var getContent = await getResponse.Content.ReadAsStringAsync();
            getContent.Should().Contain(addrId);

            // 3. Delete Address
            var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/users/me/addresses/{addrId}");
            deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var deleteResponse = await _client.SendAsync(deleteRequest);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task AddAndGetFavorites_WithComposition_ShouldReturnAggregatedData()
        {
            // Arrange
            var token = await RegisterAndLoginAsync("favtest@example.com", "Favorite123!");
            var vendorId = "vendor_101"; // Burger Point in mock data

            // 1. Add Favorite
            var addRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/me/favorites/{vendorId}");
            addRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var addResponse = await _client.SendAsync(addRequest);
            addResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // 2. Get Favorites
            var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me/favorites");
            getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Act
            var response = await _client.SendAsync(getRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"vendor_id\":\"vendor_101\"");
            content.Should().Contain("\"name\":\"Burger Point\"");
        }

        [Fact]
        public async Task RemoveFavorite_ShouldReturn204()
        {
            // Arrange
            var token = await RegisterAndLoginAsync("removetest@example.com", "Remove123!");
            var vendorId = "vendor_101";

            // Add first
            var addRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/me/favorites/{vendorId}");
            addRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await _client.SendAsync(addRequest);

            // Act: Remove
            var removeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/users/me/favorites/{vendorId}");
            removeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(removeRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // --- Helpers ---

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
