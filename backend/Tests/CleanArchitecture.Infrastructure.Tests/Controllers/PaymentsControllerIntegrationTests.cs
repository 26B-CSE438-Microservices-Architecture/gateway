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
    public class PaymentsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public PaymentsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task FullPaymentFlow_ShouldWork()
        {
            // Arrange
            var token = await RegisterAndLoginAsync();
            var initRequest = new
            {
                orderId = "ord_test_001",
                amount = 15000,
                currency = "TRY",
                callbackUrl = "https://order-service/callback/{paymentId}"
            };

            // 1. Initialize Payment
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            request.Content = JsonContent.Create(initRequest);

            var initResponse = await _client.SendAsync(request);
            initResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var initContent = await initResponse.Content.ReadAsStringAsync();
            var initData = JsonDocument.Parse(initContent);
            var paymentId = initData.RootElement.GetProperty("payment").GetProperty("id").GetString();
            var formToken = initData.RootElement.GetProperty("checkoutForm").GetProperty("token").GetString();

            // 2. Callback (simulate user finishing iyzico form)
            var callbackRequest = new { token = formToken };
            var callbackResponse = await _client.PostAsJsonAsync($"/api/v1/payments/{paymentId}/checkout-form/callback", callbackRequest);
            callbackResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var callbackContent = await callbackResponse.Content.ReadAsStringAsync();
            callbackContent.Should().Contain("\"status\":\"AUTHORIZED\"");

            // 3. Capture
            var captureRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/payments/{paymentId}/capture");
            captureRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            captureRequest.Content = JsonContent.Create(new { amount = 15000 });
            
            var captureResponse = await _client.SendAsync(captureRequest);
            captureResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var captureContent = await captureResponse.Content.ReadAsStringAsync();
            captureContent.Should().Contain("\"status\":\"CAPTURED\"");
        }

        [Fact]
        public async Task Initialize_WithoutIdempotency_ShouldReturn400()
        {
            // Arrange
            var token = await RegisterAndLoginAsync();
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new { orderId = "ord_fail", amount = 100 });

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("MISSING_IDEMPOTENCY_KEY");
        }

        private async Task<string> RegisterAndLoginAsync()
        {
            var email = $"paytest_{Guid.NewGuid():N}@example.com";
            var password = "Password123!";
            await _client.PostAsJsonAsync("/api/v1/auth/register", new { email, password, name = "Pay", surname = "Test" });
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("access_token").GetString()!;
        }
    }
}
