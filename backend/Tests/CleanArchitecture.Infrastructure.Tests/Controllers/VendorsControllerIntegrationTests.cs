using CleanArchitecture.Infrastructure.Tests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.Infrastructure.Tests.Controllers
{
    public class VendorsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public VendorsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // ========================
        // CUSTOMER DISCOVERY TESTS
        // ========================

        [Fact]
        public async Task GetVendors_ShouldReturn200()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/vendors");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"total\":");
            content.Should().Contain("\"data\":");
        }

        [Fact]
        public async Task GetVendor_WithValidId_ShouldReturn200()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/vendors/vendor_101");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"id\":\"vendor_101\"");
        }

        [Fact]
        public async Task GetNearbyVendors_ShouldReturn200()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/vendors/nearby?lat=36.88&lng=30.70");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"total\":");
        }

        // ========================
        // RESTAURANT MANAGEMENT TESTS
        // ========================

        [Fact]
        public async Task CreateVendor_WithValidData_ShouldReturn201()
        {
            // Arrange
            var request = new
            {
                name = "Integration Test Restaurant",
                description = "Test Description",
                address_text = "Test Address",
                latitude = 36.5,
                longitude = 30.5,
                min_order_amount = 100.0,
                delivery_fee = 10.0
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/vendors", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"id\":\"vendor_");
        }

        [Fact]
        public async Task UpdateVendorStatus_ShouldReturn200()
        {
            // Arrange
            var request = new { status = "Busy" };

            // Act
            var response = await _client.PatchAsJsonAsync("/api/v1/vendors/vendor_101/status", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // ========================
        // MENU & PRODUCT TESTS
        // ========================

        [Fact]
        public async Task CreateCategory_ShouldReturn200()
        {
            // Arrange
            var request = new { name = "Desserts", display_order = 1 };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/vendors/vendor_101/categories", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"id\":\"cat_");
        }

        [Fact]
        public async Task CreateProduct_ShouldReturn200()
        {
            // Arrange
            var request = new 
            { 
                name = "Test Product", 
                description = "Description",
                price = 50.0 
            };
            // section_1 is a default in mock data
            var categoryId = "section_1";

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/categories/{categoryId}/products", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"id\":\"prod_");
        }

        [Fact]
        public async Task UpdateProductStock_ShouldReturn200()
        {
            // Arrange
            var request = new { is_available = false };
            var productId = "prod_1";

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/v1/products/{productId}/stock", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}