using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public OrderService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("order");
            _httpContextAccessor = httpContextAccessor;
        }

        // ─── Helper ──────────────────────────────────────────────────────────────

        private HttpRequestMessage BuildRequest(HttpMethod method, string path, object body = null)
        {
            var req = new HttpRequestMessage(method, path);
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx != null)
            {
                // Order Service uses Authorization: Bearer JWT
                var auth = ctx.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(auth))
                    req.Headers.TryAddWithoutValidation("Authorization", auth);

                // Forward idempotency key if present
                var idempotencyKey = ctx.Request.Headers["Idempotency-Key"].ToString();
                if (!string.IsNullOrEmpty(idempotencyKey))
                    req.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            }
            if (body != null)
                req.Content = JsonContent.Create(body, options: _jsonOpts);
            return req;
        }

        private async Task<T> SendAsync<T>(HttpRequestMessage req)
        {
            var response = await _httpClient.SendAsync(req);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new NotFoundException("NOT_FOUND", "Resource not found on Order Service");

            if (!response.IsSuccessStatusCode)
            {
                // Try to extract error message from downstream response
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new ApiException("ORDER_SERVICE_ERROR",
                    $"Order Service returned {(int)response.StatusCode}: {errorBody}");
            }
            return await response.Content.ReadFromJsonAsync<T>(_jsonOpts);
        }

        // ─── Cart Management ──────────────────────────────────────────────────────

        public async Task<CartResponse> GetCartAsync(string userId)
        {
            var req = BuildRequest(HttpMethod.Get, "cart");
            return await SendAsync<CartResponse>(req);
        }

        public async Task<CartResponse> AddToCartAsync(string userId, AddCartItemRequest request)
        {
            var req = BuildRequest(HttpMethod.Post, "cart/items", request);
            return await SendAsync<CartResponse>(req);
        }

        public async Task<CartResponse> UpdateCartItemAsync(string userId, string productId, UpdateCartItemRequest request)
        {
            var req = BuildRequest(HttpMethod.Put, $"cart/items/{productId}", request);
            return await SendAsync<CartResponse>(req);
        }

        public async Task<CartResponse> RemoveFromCartAsync(string userId, string productId)
        {
            var req = BuildRequest(HttpMethod.Delete, $"cart/items/{productId}");
            return await SendAsync<CartResponse>(req);
        }

        public async Task ClearCartAsync(string userId)
        {
            var req = BuildRequest(HttpMethod.Delete, "cart");
            var response = await _httpClient.SendAsync(req);
            // Accept 204 No Content or 200 OK
        }

        public async Task<OrderResponse> CheckoutAsync(string userId, CheckoutRequest request, string idempotencyKey)
        {
            var req = BuildRequest(HttpMethod.Post, "cart/checkout", request);
            // Also ensure idempotency key is set on the request (BuildRequest reads from HttpContext but let's also add explicitly)
            if (!string.IsNullOrEmpty(idempotencyKey) && !req.Headers.Contains("Idempotency-Key"))
                req.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            return await SendAsync<OrderResponse>(req);
        }

        // ─── Customer Order Management ────────────────────────────────────────────

        public async Task<OrderResponse> GetOrderByIdAsync(string userId, string orderId)
        {
            var req = BuildRequest(HttpMethod.Get, $"orders/{orderId}");
            return await SendAsync<OrderResponse>(req);
        }

        public async Task<PagedResponse<OrderResponse>> GetMyOrdersAsync(string userId, string status, int page, int size)
        {
            var query = $"orders/my?page={page}&size={size}";
            if (!string.IsNullOrEmpty(status)) query += $"&status={status}";
            var req = BuildRequest(HttpMethod.Get, query);
            return await SendAsync<PagedResponse<OrderResponse>>(req);
        }

        public async Task<OrderResponse> CancelOrderAsync(string userId, string orderId)
        {
            var req = BuildRequest(HttpMethod.Post, $"orders/{orderId}/cancel");
            return await SendAsync<OrderResponse>(req);
        }

        public async Task<CartResponse> ReorderAsync(string userId, string orderId)
        {
            var req = BuildRequest(HttpMethod.Post, $"orders/{orderId}/reorder");
            return await SendAsync<CartResponse>(req);
        }

        public async Task<OrderResponse> RequestRefundAsync(string userId, string orderId)
        {
            var req = BuildRequest(HttpMethod.Post, $"orders/{orderId}/request-refund");
            return await SendAsync<OrderResponse>(req);
        }

        // ─── Restaurant Order Management ──────────────────────────────────────────

        public async Task<PagedResponse<OrderResponse>> GetRestaurantOrdersAsync(string restaurantId, string status, int page, int size)
        {
            var query = $"orders/restaurant?page={page}&size={size}";
            if (!string.IsNullOrEmpty(status)) query += $"&status={status}";
            var req = BuildRequest(HttpMethod.Get, query);
            return await SendAsync<PagedResponse<OrderResponse>>(req);
        }

        public async Task<OrderResponse> ConfirmOrderAsync(string restaurantId, string orderId)
        {
            var req = BuildRequest(HttpMethod.Patch, $"orders/restaurant/{orderId}/confirm");
            return await SendAsync<OrderResponse>(req);
        }

        public async Task<OrderResponse> RejectOrderAsync(string restaurantId, string orderId)
        {
            var req = BuildRequest(HttpMethod.Patch, $"orders/restaurant/{orderId}/reject");
            return await SendAsync<OrderResponse>(req);
        }

        public async Task<OrderResponse> UpdateOrderStatusAsync(string restaurantId, string orderId, string status)
        {
            var body = new { status };
            var req = BuildRequest(HttpMethod.Patch, $"orders/restaurant/{orderId}/status", body);
            return await SendAsync<OrderResponse>(req);
        }

        // ─── Internal Operations ──────────────────────────────────────────────────

        public async Task<OrderResponse> ProcessPaymentCallbackAsync(string orderId, string status)
        {
            var body = new { status };
            var req = new HttpRequestMessage(HttpMethod.Post, $"internal/orders/{orderId}/payment-callback");
            // Internal endpoint uses X-Internal-Secret, not JWT
            req.Headers.TryAddWithoutValidation("X-Internal-Secret", "change-me-in-production");
            req.Content = JsonContent.Create(body, options: _jsonOpts);
            return await SendAsync<OrderResponse>(req);
        }
    }
}
