using CleanArchitecture.Infrastructure.Tests.Helpers;
using FluentAssertions;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.Infrastructure.Tests.Controllers
{
    public class OrderFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public OrderFlowIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task FullOrderLifecycle_ShouldWork()
        {
            // Arrange
            var token = await RegisterAndLoginAsync("customer@example.com", "Customer");
            
            // Login as Seeded SysAdmin for restaurant confirmation
            var adminLoginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "sysadmin@trendyolgo.com", password = "123Pa$$word!" });
            var adminData = await adminLoginResponse.Content.ReadFromJsonAsync<JsonElement>();
            var adminToken = adminData.GetProperty("access_token").GetString()!;
            
            // 1. Add to Cart
            var addResponse = await PostWithAuthAsync("/api/v1/cart/items", new { productId = "p_1", quantity = 2 }, token);
            addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 2. Checkout
            var checkoutRequest = new
            {
                deliveryAddress = new { street = "123 Test St", city = "Istanbul" },
                paymentMethod = "CREDIT_CARD",
                notes = "Hold test"
            };
            var checkoutMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cart/checkout");
            checkoutMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            checkoutMsg.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            checkoutMsg.Content = JsonContent.Create(checkoutRequest);
            
            var checkoutResponse = await _client.SendAsync(checkoutMsg);
            checkoutResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var orderId = (await checkoutResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // 3. Internal Callback (Hold Confirmed)
            var callbackMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/internal/orders/{orderId}/payment-callback");
            callbackMsg.Headers.Add("X-Internal-Secret", "change-me-in-production");
            callbackMsg.Content = JsonContent.Create(new { status = "HOLD_CONFIRMED" });
            
            var callbackResponse = await _client.SendAsync(callbackMsg);
            callbackResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 4. Restaurant Confirm (Requires Admin/RestaurantOwner role)
            var confirmMsg = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/orders/restaurant/{orderId}/confirm");
            confirmMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            
            var confirmResponse = await _client.SendAsync(confirmMsg);
            confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // 5. Final Status Check
            var checkResponse = await GetWithAuthAsync($"/api/v1/orders/{orderId}", token);
            var finalStatus = (await checkResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString();
            finalStatus.Should().Be("PAID");
        }

        private async Task<HttpResponseMessage> PostWithAuthAsync(string url, object body, string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(body);
            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> GetWithAuthAsync(string url, string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _client.SendAsync(request);
        }

        private async Task<string> RegisterAndLoginAsync(string email, string name)
        {
            var password = "Password123!";
            // Note: In our mock, registration might auto-assign roles or we use seeded users.
            // For integration tests, we'll assume the login helper returns token with correct claims.
            await _client.PostAsJsonAsync("/api/v1/auth/register", new { email, password, name, surname = "Test" });
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("access_token").GetString()!;
        }
    }
}
