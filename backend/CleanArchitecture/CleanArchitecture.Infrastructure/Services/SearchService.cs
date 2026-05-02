using CleanArchitecture.Core.DTOs.Search;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class SearchService : ISearchService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public SearchService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("restaurant");
            _httpContextAccessor = httpContextAccessor;
        }

        private class RsRestaurant
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string LogoUrl { get; set; }
            public string Status { get; set; }
            public double? DistanceKm { get; set; }
        }

        public async Task<DiscoveryResponse> GetDiscoveryAsync()
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, "api/v1/restaurants?page=1&size=20");
                var response = await _httpClient.SendAsync(req);
                if (!response.IsSuccessStatusCode) return FallbackDiscovery();

                var restaurants = await response.Content.ReadFromJsonAsync<List<RsRestaurant>>(_jsonOpts)
                                  ?? new List<RsRestaurant>();

                var chainItems = restaurants.Take(8).Select(r => new DiscoveryItemDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    LogoUrl = r.LogoUrl
                }).ToList();

                return new DiscoveryResponse
                {
                    Sections = new List<DiscoverySectionDto>
                    {
                        new DiscoverySectionDto { Title = "Restoranlar", Type = "HORIZONTAL_LIST", Items = chainItems },
                        new DiscoverySectionDto
                        {
                            Title = "Mutfaklar",
                            Type = "GRID",
                            Items = new List<DiscoveryItemDto>
                            {
                                new DiscoveryItemDto { Id = "cat_1", Name = "Doner", ImageUrl = "https://cdn.app.com/cuisines/doner.png", ColorCode = "#FDECEC" },
                                new DiscoveryItemDto { Id = "cat_2", Name = "Hamburger", ImageUrl = "https://cdn.app.com/cuisines/burger.png", ColorCode = "#FFF4E5" },
                                new DiscoveryItemDto { Id = "cat_3", Name = "Pizza", ImageUrl = "https://cdn.app.com/cuisines/pizza.png", ColorCode = "#EEF0FD" },
                                new DiscoveryItemDto { Id = "cat_4", Name = "Sushi", ImageUrl = "https://cdn.app.com/cuisines/sushi.png", ColorCode = "#FDF4EE" }
                            }
                        }
                    }
                };
            }
            catch { return FallbackDiscovery(); }
        }

        public async Task<SearchResponse> SearchAsync(string keyword, double? lat, double? lng)
        {
            try
            {
                var query = "api/v1/restaurants?page=1&size=50";
                if (lat.HasValue && lng.HasValue) query += $"&lat={lat}&lng={lng}";

                var req = new HttpRequestMessage(HttpMethod.Get, query);
                var response = await _httpClient.SendAsync(req);
                if (!response.IsSuccessStatusCode)
                    return new SearchResponse { TotalCount = 0, Vendors = new List<SearchVendorDto>(), Products = new List<SearchProductDto>() };

                var restaurants = await response.Content.ReadFromJsonAsync<List<RsRestaurant>>(_jsonOpts)
                                  ?? new List<RsRestaurant>();

                if (!string.IsNullOrEmpty(keyword))
                {
                    var lower = keyword.ToLower();
                    restaurants = restaurants.Where(r => r.Name != null && r.Name.ToLower().Contains(lower)).ToList();
                }

                var vendors = restaurants.Select(r => new SearchVendorDto
                {
                    Id = r.Id, Name = r.Name, Rating = 0, IsSponsored = false,
                    ImageUrl = r.LogoUrl, Eta = "20-30 dk"
                }).ToList();

                return new SearchResponse { TotalCount = vendors.Count, Vendors = vendors, Products = new List<SearchProductDto>() };
            }
            catch
            {
                return new SearchResponse { TotalCount = 0, Vendors = new List<SearchVendorDto>(), Products = new List<SearchProductDto>() };
            }
        }

        private static DiscoveryResponse FallbackDiscovery() => new DiscoveryResponse
        {
            Sections = new List<DiscoverySectionDto>
            {
                new DiscoverySectionDto
                {
                    Title = "Mutfaklar", Type = "GRID",
                    Items = new List<DiscoveryItemDto>
                    {
                        new DiscoveryItemDto { Id = "cat_1", Name = "Doner", ImageUrl = "https://cdn.app.com/cuisines/doner.png", ColorCode = "#FDECEC" },
                        new DiscoveryItemDto { Id = "cat_2", Name = "Hamburger", ImageUrl = "https://cdn.app.com/cuisines/burger.png", ColorCode = "#FFF4E5" }
                    }
                }
            }
        };
    }
}
