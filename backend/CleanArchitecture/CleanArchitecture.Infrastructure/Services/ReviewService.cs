using CleanArchitecture.Core.DTOs.Review;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class ReviewService : IReviewService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReviewService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("restaurant");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PagedReviewsResponse> GetVendorReviewsAsync(string vendorId, int page, int limit)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"internal/catalog/restaurants/{vendorId}/reviews?page={page}&size={limit}");
            // Forward authorization if present
            var auth = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(auth)) req.Headers.TryAddWithoutValidation("Authorization", auth);

            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                return new PagedReviewsResponse { Page = page, Limit = limit, Total = 0, Data = new List<ReviewDto>() };
            }

            return await response.Content.ReadFromJsonAsync<PagedReviewsResponse>() ?? new PagedReviewsResponse();
        }
    }
}
