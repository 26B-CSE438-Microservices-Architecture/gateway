using CleanArchitecture.Core.DTOs.Campaign;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class CampaignService : ICampaignService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CampaignService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("restaurant");
            _httpContextAccessor = httpContextAccessor;
        }

        private void AddAuthHeader(HttpRequestMessage req)
        {
            var auth = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(auth)) req.Headers.TryAddWithoutValidation("Authorization", auth);
        }

        public async Task<PagedCampaignsResponse<CampaignDto>> GetCampaignsAsync(int page, int limit)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"internal/catalog/campaigns?page={page}&size={limit}");
            AddAuthHeader(req);

            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                return new PagedCampaignsResponse<CampaignDto> { Page = page, Limit = limit, Total = 0, Data = new List<CampaignDto>() };
            }

            return await response.Content.ReadFromJsonAsync<PagedCampaignsResponse<CampaignDto>>() ?? new PagedCampaignsResponse<CampaignDto>();
        }

        public async Task<PagedCampaignsResponse<VendorCampaignDto>> GetVendorCampaignsAsync(string vendorId, int page, int limit)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"internal/catalog/restaurants/{vendorId}/campaigns?page={page}&size={limit}");
            AddAuthHeader(req);

            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                return new PagedCampaignsResponse<VendorCampaignDto> { Page = page, Limit = limit, Total = 0, Data = new List<VendorCampaignDto>() };
            }

            return await response.Content.ReadFromJsonAsync<PagedCampaignsResponse<VendorCampaignDto>>() ?? new PagedCampaignsResponse<VendorCampaignDto>();
        }
    }
}
