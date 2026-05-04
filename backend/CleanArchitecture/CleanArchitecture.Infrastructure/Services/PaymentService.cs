using CleanArchitecture.Core.DTOs.Payment;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public PaymentService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("payment");
            _httpContextAccessor = httpContextAccessor;
        }

        // ─── Helper ──────────────────────────────────────────────────────────────

        private HttpRequestMessage BuildRequest(HttpMethod method, string path, object body = null)
        {
            var req = new HttpRequestMessage(method, path);
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx != null)
            {
                // Payment Service uses Authorization: Bearer JWT
                var auth = ctx.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(auth))
                    req.Headers.TryAddWithoutValidation("Authorization", auth);

                // Forward Idempotency-Key if present
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
                throw new NotFoundException("PAYMENT_NOT_FOUND", "Payment not found on Payment Service");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                var message = errorBody;
                var errorCode = "PAYMENT_SERVICE_ERROR";
                try 
                {
                    using var doc = JsonDocument.Parse(errorBody);
                    if (doc.RootElement.TryGetProperty("code", out var codeProp))
                    {
                        errorCode = codeProp.GetString();
                    }

                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        message = msgProp.GetString();
                    }
                    else if (doc.RootElement.TryGetProperty("error", out var errProp))
                    {
                        if (errProp.ValueKind == JsonValueKind.String)
                        {
                            message = errProp.GetString();
                        }
                        else if (errProp.ValueKind == JsonValueKind.Object)
                        {
                            if (errProp.TryGetProperty("code", out var innerCode)) errorCode = innerCode.GetString();
                            if (errProp.TryGetProperty("message", out var innerMsg)) message = innerMsg.GetString();
                        }
                    }
                }
                catch { /* Not JSON or missing expected properties */ }

                throw new ApiException(errorCode, message);
            }
            return await response.Content.ReadFromJsonAsync<T>(_jsonOpts);
        }

        // ─── Payment Endpoints ────────────────────────────────────────────────────

        public async Task<PaymentInitResponse> InitializePaymentAsync(string userId, PaymentInitRequest request, string idempotencyKey)
        {
            var req = BuildRequest(HttpMethod.Post, "payments", request);
            // Also ensure idempotency key is set explicitly
            if (!string.IsNullOrEmpty(idempotencyKey) && !req.Headers.Contains("Idempotency-Key"))
                req.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            return await SendAsync<PaymentInitResponse>(req);
        }

        public async Task<PaymentResponse> ProcessCallbackAsync(string paymentId, string token)
        {
            var body = new { token };
            // Callback comes from browser redirect — no auth header needed
            var req = new HttpRequestMessage(HttpMethod.Post, $"payments/{paymentId}/checkout-form/callback");
            req.Content = JsonContent.Create(body, options: _jsonOpts);
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PaymentCallbackResponse>(_jsonOpts);
            return result?.Payment;
        }

        private class PaymentCallbackResponse
        {
            public PaymentResponse Payment { get; set; }
        }

        public async Task<PaymentResponse> CapturePaymentAsync(string paymentId, int amount)
        {
            var body = new { amount };
            var req = BuildRequest(HttpMethod.Post, $"payments/{paymentId}/capture", body);
            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new ApiException("CAPTURE_FAILED", $"Payment capture failed: {errorBody}");
            }
            var result = await response.Content.ReadFromJsonAsync<PaymentCallbackResponse>(_jsonOpts);
            return result?.Payment;
        }

        public async Task<PaymentResponse> CancelPaymentAsync(string paymentId, string reason)
        {
            var body = new { reason };
            var req = BuildRequest(HttpMethod.Post, $"payments/{paymentId}/cancel", body);
            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new ApiException("CANCEL_FAILED", $"Payment cancel failed: {errorBody}");
            }
            var result = await response.Content.ReadFromJsonAsync<PaymentCallbackResponse>(_jsonOpts);
            return result?.Payment;
        }

        public async Task<PaymentResponse> GetPaymentByIdAsync(string paymentId)
        {
            var req = BuildRequest(HttpMethod.Get, $"payments/{paymentId}");
            var result = await SendAsync<PaymentCallbackResponse>(req);
            return result?.Payment;
        }

        public async Task<List<PaymentResponse>> GetPaymentsByOrderIdAsync(string orderId)
        {
            var req = BuildRequest(HttpMethod.Get, $"payments?orderId={orderId}");
            var result = await SendAsync<PaymentListResponse>(req);
            return result?.Payments ?? new List<PaymentResponse>();
        }

        private class PaymentListResponse
        {
            public List<PaymentResponse> Payments { get; set; }
        }
    }
}
