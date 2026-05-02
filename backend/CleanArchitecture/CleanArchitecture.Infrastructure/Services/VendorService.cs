using CleanArchitecture.Core.DTOs.Vendor;
using CleanArchitecture.Core.Exceptions;
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
    public class VendorService : IVendorService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public VendorService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("restaurant");
            _httpContextAccessor = httpContextAccessor;
        }

        // ─── Helper ──────────────────────────────────────────────────────────────

        private HttpRequestMessage BuildRequest(HttpMethod method, string path, object body = null)
        {
            var req = new HttpRequestMessage(method, path);
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx != null)
            {
                var auth = ctx.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(auth))
                    req.Headers.TryAddWithoutValidation("Authorization", auth);
            }
            if (body != null)
                req.Content = JsonContent.Create(body, options: _jsonOpts);
            return req;
        }

        private async Task<T> SendAsync<T>(HttpRequestMessage req)
        {
            var response = await _httpClient.SendAsync(req);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new NotFoundException("NOT_FOUND", "Resource not found on Restaurant Service");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(_jsonOpts);
        }

        private async Task<bool> SendVoidAsync(HttpRequestMessage req)
        {
            var response = await _httpClient.SendAsync(req);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;
            return response.IsSuccessStatusCode;
        }

        // ─── Restaurant Service API DTO (intermediate) ────────────────────────────

        private class RsRestaurant
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string AddressText { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string LogoUrl { get; set; }
            public double MinOrderAmount { get; set; }
            public double DeliveryFee { get; set; }
            public bool IsActive { get; set; }
            public string Status { get; set; }
            public string OpeningTime { get; set; }
            public string ClosingTime { get; set; }
            public double? DistanceKm { get; set; }
        }

        private class RsMenuResponse
        {
            public string RestaurantId { get; set; }
            public string RestaurantName { get; set; }
            public List<RsCategory> Categories { get; set; }
        }

        private class RsCategory
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int DisplayOrder { get; set; }
            public List<RsProduct> Products { get; set; }
        }

        private class RsProduct
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public double Price { get; set; }
            public bool IsAvailable { get; set; }
            public string ImageUrl { get; set; }
        }

        private class RsCreatedId
        {
            public string Id { get; set; }
        }

        // ─── Mapping Helpers ──────────────────────────────────────────────────────

        private static VendorDetailDto MapDetail(RsRestaurant r)
        {
            return new VendorDetailDto
            {
                Id = r.Id,
                Name = r.Name,
                Kind = "RESTAURANT",
                Description = r.Description,
                AddressText = r.AddressText,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                LogoUrl = r.LogoUrl,
                Status = r.Status,
                Rating = 0,
                ReviewCount = 0,
                DistanceKm = r.DistanceKm ?? 0,
                CampaignBadges = new List<string>(),
                WorkingHours = new WorkingHoursDto
                {
                    Open = r.OpeningTime ?? "00:00",
                    Close = r.ClosingTime ?? "23:59",
                    IsOpen = r.Status == "Open"
                },
                DeliveryInfo = new DeliveryInfoDto
                {
                    MinimumBasketAmount = r.MinOrderAmount,
                    DeliveryFee = r.DeliveryFee,
                    EtaRange = "20-30 dk"
                },
                MenuSections = new List<MenuSectionDto>()
            };
        }

        private static VendorSummaryDto MapSummary(RsRestaurant r) => MapDetail(r);

        // ─── Public API ──────────────────────────────────────────────────────────

        public async Task<PagedVendorsResponse> GetVendorsAsync(int page, int limit)
        {
            var req = BuildRequest(HttpMethod.Get, $"api/v1/restaurants?page={page}&size={limit}");
            List<RsRestaurant> list;
            try { list = await SendAsync<List<RsRestaurant>>(req); }
            catch { list = new List<RsRestaurant>(); }

            return new PagedVendorsResponse
            {
                Page = page,
                Limit = limit,
                Total = list.Count,
                Data = list.Select(MapSummary).ToList()
            };
        }

        public async Task<VendorDetailDto> GetVendorByIdAsync(string vendorId)
        {
            var req = BuildRequest(HttpMethod.Get, $"api/v1/restaurants/{vendorId}");
            var restaurant = await SendAsync<RsRestaurant>(req);

            // Fetch menu separately
            try
            {
                var menuReq = BuildRequest(HttpMethod.Get, $"api/v1/restaurants/{vendorId}/menu");
                var menu = await SendAsync<RsMenuResponse>(menuReq);
                var detail = MapDetail(restaurant);
                detail.MenuSections = menu?.Categories?.Select(c => new MenuSectionDto
                {
                    Id = c.Id,
                    Title = c.Name,
                    Products = c.Products?.Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        IsAvailable = p.IsAvailable,
                        ImageUrl = p.ImageUrl
                    }).ToList() ?? new List<ProductDto>()
                }).ToList() ?? new List<MenuSectionDto>();
                return detail;
            }
            catch
            {
                return MapDetail(restaurant);
            }
        }

        public async Task<PagedVendorsResponse> GetNearbyVendorsAsync(double lat, double lng, double radiusKm)
        {
            var req = BuildRequest(HttpMethod.Get, $"api/v1/restaurants/nearby?lat={lat}&lng={lng}&radius={radiusKm}");
            List<RsRestaurant> list;
            try { list = await SendAsync<List<RsRestaurant>>(req); }
            catch { list = new List<RsRestaurant>(); }

            return new PagedVendorsResponse
            {
                Page = 1,
                Limit = list.Count,
                Total = list.Count,
                Data = list.Select(MapSummary).ToList()
            };
        }

        public async Task<List<VendorLookupItemDto>> LookupVendorsAsync(List<string> vendorIds)
        {
            // Restaurant Service has no bulk endpoint — call individually in parallel
            var tasks = vendorIds.Select(async id =>
            {
                try
                {
                    var req = BuildRequest(HttpMethod.Get, $"api/v1/restaurants/{id}");
                    var r = await SendAsync<RsRestaurant>(req);
                    return new VendorLookupItemDto { VendorId = r.Id, Name = r.Name, ImageUrl = r.LogoUrl };
                }
                catch { return null; }
            });

            var results = await Task.WhenAll(tasks);
            return results.Where(r => r != null).ToList();
        }

        public async Task<string> CreateVendorAsync(CreateVendorDto request)
        {
            var body = new
            {
                name = request.Name,
                description = request.Description,
                addressText = request.AddressText,
                latitude = request.Latitude,
                longitude = request.Longitude,
                logoUrl = request.LogoUrl,
                minOrderAmount = request.MinOrderAmount,
                deliveryFee = request.DeliveryFee,
                openingTime = request.OpeningTime,
                closingTime = request.ClosingTime
            };
            var req = BuildRequest(HttpMethod.Post, "api/v1/restaurants", body);
            var result = await SendAsync<RsCreatedId>(req);
            return result?.Id ?? Guid.NewGuid().ToString();
        }

        public async Task<bool> UpdateVendorAsync(string vendorId, UpdateVendorDto request)
        {
            var body = new
            {
                name = request.Name,
                description = request.Description,
                addressText = request.AddressText,
                latitude = request.Latitude,
                longitude = request.Longitude,
                logoUrl = request.LogoUrl,
                minOrderAmount = request.MinOrderAmount,
                deliveryFee = request.DeliveryFee
            };
            var req = BuildRequest(HttpMethod.Put, $"api/v1/restaurants/{vendorId}", body);
            return await SendVoidAsync(req);
        }

        public async Task<bool> UpdateVendorStatusAsync(string vendorId, UpdateStatusDto request)
        {
            var body = new { status = request.Status };
            var req = BuildRequest(HttpMethod.Patch, $"api/v1/restaurants/{vendorId}/status", body);
            return await SendVoidAsync(req);
        }

        public async Task<bool> DeleteVendorAsync(string vendorId)
        {
            var req = BuildRequest(HttpMethod.Delete, $"api/v1/restaurants/{vendorId}");
            return await SendVoidAsync(req);
        }

        public async Task<string> CreateCategoryAsync(string vendorId, CreateCategoryDto request)
        {
            var body = new { name = request.Name, displayOrder = request.DisplayOrder };
            var req = BuildRequest(HttpMethod.Post, $"api/v1/restaurants/{vendorId}/categories", body);
            var result = await SendAsync<RsCreatedId>(req);
            return result?.Id ?? Guid.NewGuid().ToString();
        }

        public async Task<bool> UpdateCategoryAsync(string categoryId, UpdateCategoryDto request)
        {
            var body = new { name = request.Name, displayOrder = request.DisplayOrder };
            var req = BuildRequest(HttpMethod.Put, $"api/v1/categories/{categoryId}", body);
            return await SendVoidAsync(req);
        }

        public async Task<bool> DeleteCategoryAsync(string categoryId)
        {
            var req = BuildRequest(HttpMethod.Delete, $"api/v1/categories/{categoryId}");
            return await SendVoidAsync(req);
        }

        public async Task<string> CreateProductAsync(string categoryId, CreateProductDto request)
        {
            var body = new
            {
                name = request.Name,
                description = request.Description,
                price = request.Price,
                imageUrl = request.ImageUrl
            };
            var req = BuildRequest(HttpMethod.Post, $"api/v1/categories/{categoryId}/products", body);
            var result = await SendAsync<RsCreatedId>(req);
            return result?.Id ?? Guid.NewGuid().ToString();
        }

        public async Task<bool> UpdateProductAsync(string productId, UpdateProductDto request)
        {
            var body = new
            {
                name = request.Name,
                description = request.Description,
                price = request.Price,
                imageUrl = request.ImageUrl
            };
            var req = BuildRequest(HttpMethod.Put, $"api/v1/products/{productId}", body);
            return await SendVoidAsync(req);
        }

        public async Task<bool> UpdateProductStockAsync(string productId, UpdateStockDto request)
        {
            var body = new { isAvailable = request.IsAvailable };
            var req = BuildRequest(HttpMethod.Patch, $"api/v1/products/{productId}/stock", body);
            return await SendVoidAsync(req);
        }

        public async Task<bool> DeleteProductAsync(string productId)
        {
            var req = BuildRequest(HttpMethod.Delete, $"api/v1/products/{productId}");
            return await SendVoidAsync(req);
        }
    }
}
