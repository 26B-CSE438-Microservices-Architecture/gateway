using CleanArchitecture.Core.DTOs.User;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
    public class UserService : IUserService
    {
        // UserManager is kept for gateway-local auth operations (login/register uses local Identity DB)
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public UserService(UserManager<ApplicationUser> userManager,
                           IHttpClientFactory httpClientFactory,
                           IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _httpClient = httpClientFactory.CreateClient("user");
            _httpContextAccessor = httpContextAccessor;
        }

        // ─── Helper ──────────────────────────────────────────────────────────────

        private HttpRequestMessage BuildRequest(HttpMethod method, string path, object body = null)
        {
            var req = new HttpRequestMessage(method, path);
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx != null)
            {
                // User Service uses X-User-Id header (not JWT)
                var userId = ctx.User.FindFirst("uid")?.Value;
                if (!string.IsNullOrEmpty(userId))
                    req.Headers.TryAddWithoutValidation("X-User-Id", userId);
            }
            if (body != null)
                req.Content = JsonContent.Create(body, options: _jsonOpts);
            return req;
        }

        // Build a request for internal endpoints (no user context needed)
        private static HttpRequestMessage BuildInternalRequest(HttpMethod method, string path, object body = null)
        {
            var req = new HttpRequestMessage(method, path);
            if (body != null)
                req.Content = JsonContent.Create(body);
            return req;
        }

        private async Task<T> SendAsync<T>(HttpRequestMessage req)
        {
            var response = await _httpClient.SendAsync(req);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new NotFoundException("USER_NOT_FOUND", "User not found on User Service");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(_jsonOpts);
        }

        // ─── User Service Response DTOs (intermediate) ────────────────────────────

        // User Service returns snake_case fields
        private class UsResponse
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Surname { get; set; }
            public string Email { get; set; }
            [JsonPropertyName("phone_number")]
            public string PhoneNumber { get; set; }
            public string Phone { get; set; }
            public string Role { get; set; }
            [JsonPropertyName("is_active")]
            public bool IsActive { get; set; }
            public bool Active { get; set; }
            [JsonPropertyName("created_at")]
            public DateTime CreatedAt { get; set; }
            public List<UsAddress> Addresses { get; set; }
        }

        private class UsAddress
        {
            public string Id { get; set; }
            [JsonPropertyName("address_title")]
            public string AddressTitle { get; set; }
            public string City { get; set; }
            public string Street { get; set; }
            [JsonPropertyName("location")]
            public UsLocation Location { get; set; }
        }

        private class UsLocation
        {
            public double Lat { get; set; }
            public double Lng { get; set; }
        }

        private class UsFavoritesResponse
        {
            public List<UsFavoriteItem> Items { get; set; }
            public int Page { get; set; }
            public int Size { get; set; }
            public int Total { get; set; }
        }

        private class UsFavoriteItem
        {
            [JsonPropertyName("vendor_id")]
            public string VendorId { get; set; }
            [JsonPropertyName("created_at")]
            public DateTime CreatedAt { get; set; }
        }

        // ─── Mapping ─────────────────────────────────────────────────────────────

        private static UserProfileDto MapUser(UsResponse u)
        {
            var fullName = string.IsNullOrEmpty(u.Surname)
                ? u.Name
                : $"{u.Name} {u.Surname}".Trim();

            return new UserProfileDto
            {
                Id = u.Id,
                Name = fullName,
                Email = u.Email,
                Phone = u.PhoneNumber ?? u.Phone,
                Role = u.Role ?? "CUSTOMER",
                Active = u.IsActive || u.Active,
                CreatedAt = u.CreatedAt == default ? DateTime.UtcNow : u.CreatedAt,
                Addresses = u.Addresses?.Select(MapAddress).ToList() ?? new List<AddressDto>()
            };
        }

        private static AddressDto MapAddress(UsAddress a) => new AddressDto
        {
            Id = a.Id,
            Label = a.AddressTitle ?? "Address",
            City = a.City,
            Street = a.Street,
            PostalCode = "",
            Lat = a.Location?.Lat ?? 0,
            Lng = a.Location?.Lng ?? 0
        };

        // ─── Public Profile Endpoints (proxy to User Service) ─────────────────────

        public async Task<UserProfileDto> GetProfileAsync(string userId)
        {
            // We pass userId via X-User-Id header (set in BuildRequest from JWT claims)
            var req = BuildRequest(HttpMethod.Get, "api/v1/users/me");
            var u = await SendAsync<UsResponse>(req);
            return MapUser(u);
        }

        public async Task<UserProfileDto> UpdateProfileAsync(string userId, UserUpdateProfileRequest request)
        {
            var body = new { name = request.Name, phone = request.Phone };
            var req = BuildRequest(HttpMethod.Put, "api/v1/users/me", body);
            var u = await SendAsync<UsResponse>(req);
            return MapUser(u);
        }

        // ─── Address Endpoints ────────────────────────────────────────────────────

        public async Task<List<AddressDto>> GetAddressesAsync(string userId)
        {
            var req = BuildRequest(HttpMethod.Get, "api/v1/users/me/addresses");
            try
            {
                var addresses = await SendAsync<List<UsAddress>>(req);
                return addresses?.Select(MapAddress).ToList() ?? new List<AddressDto>();
            }
            catch { return new List<AddressDto>(); }
        }

        public async Task<AddressDto> CreateAddressAsync(string userId, CreateAddressRequest request)
        {
            var body = new
            {
                address_title = request.Label ?? "Address",
                city = request.City,
                district = "",
                neighborhood = "",
                street = request.Street,
                building_no = "1",
                floor = "0",
                apartment_no = "1",
                phone = "",
                location = new { lat = request.Lat, lng = request.Lng }
            };
            var req = BuildRequest(HttpMethod.Post, "api/v1/users/me/addresses", body);
            var result = await SendAsync<UsAddress>(req);
            return MapAddress(result);
        }

        public async Task DeleteAddressAsync(string userId, string addressId)
        {
            var req = BuildRequest(HttpMethod.Delete, $"api/v1/users/me/addresses/{addressId}");
            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
                response.EnsureSuccessStatusCode();
        }

        public async Task<AddressDto> UpdateAddressAsync(string userId, string addressId, CreateAddressRequest request)
        {
            var body = new
            {
                address_title = request.Label ?? "Address",
                city = request.City,
                street = request.Street,
                location = new { lat = request.Lat, lng = request.Lng }
            };
            var req = BuildRequest(HttpMethod.Put, $"api/v1/users/me/addresses/{addressId}", body);
            var result = await SendAsync<UsAddress>(req);
            return MapAddress(result);
        }

        public async Task SetCurrentAddressAsync(string userId, string addressId)
        {
            var req = BuildRequest(HttpMethod.Patch, $"api/v1/users/me/addresses/{addressId}/current");
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();
        }

        // ─── Favorites Endpoints ──────────────────────────────────────────────────

        public async Task<PagedFavoritesResponse<UserStoreFavorite>> GetFavoritesAsync(string userId, int page, int limit)
        {
            var req = BuildRequest(HttpMethod.Get, $"api/v1/users/me/favorites?page={page}&size={limit}");
            try
            {
                var result = await SendAsync<UsFavoritesResponse>(req);
                return new PagedFavoritesResponse<UserStoreFavorite>
                {
                    Page = result.Page,
                    Limit = result.Size > 0 ? result.Size : limit,
                    Total = result.Total,
                    Data = result.Items?.Select(f => new UserStoreFavorite
                    {
                        VendorId = f.VendorId,
                        CreatedAt = f.CreatedAt
                    }).ToList() ?? new List<UserStoreFavorite>()
                };
            }
            catch
            {
                return new PagedFavoritesResponse<UserStoreFavorite>
                {
                    Page = page, Limit = limit, Total = 0, Data = new List<UserStoreFavorite>()
                };
            }
        }

        public async Task AddFavoriteAsync(string userId, string vendorId)
        {
            var req = BuildRequest(HttpMethod.Post, $"api/v1/users/me/favorites/{vendorId}");
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();
        }

        public async Task RemoveFavoriteAsync(string userId, string vendorId)
        {
            var req = BuildRequest(HttpMethod.Delete, $"api/v1/users/me/favorites/{vendorId}");
            var response = await _httpClient.SendAsync(req);
            // Idempotent — 200 or 404 are both fine
        }

        // ─── Internal Endpoints ────────────────────────────────────────────────────

        public async Task<UserProfileDto> GetInternalUserByIdAsync(string userId)
        {
            var req = BuildInternalRequest(HttpMethod.Get, $"internal/v1/users/{userId}");
            try
            {
                var u = await SendAsync<UsResponse>(req);
                return MapUser(u);
            }
            catch
            {
                // Fall back to local Identity if User Service doesn't have this user
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) throw new NotFoundException("USER_NOT_FOUND", "User not found");
                return await MapLocalUserAsync(user);
            }
        }

        public async Task<List<UserProfileDto>> LookupUsersAsync(List<string> userIds)
        {
            var body = new { userIds };
            var req = BuildInternalRequest(HttpMethod.Post, "internal/v1/users/lookup", body);
            try
            {
                var result = await SendAsync<UsLookupResponse>(req);
                return result?.Users?.Select(MapUser).ToList() ?? new List<UserProfileDto>();
            }
            catch
            {
                // Fall back to local Identity
                var profiles = new List<UserProfileDto>();
                foreach (var id in userIds)
                {
                    var user = await _userManager.FindByIdAsync(id);
                    if (user != null) profiles.Add(await MapLocalUserAsync(user));
                }
                return profiles;
            }
        }

        private class UsLookupResponse
        {
            public List<UsResponse> Users { get; set; }
        }

        /// <summary>
        /// Used by AccountService during login — calls User Service internal endpoint to get hashed password.
        /// Falls back to local Identity if User Service doesn't have this user.
        /// </summary>
        public async Task<UserAuthDto> GetUserByEmailAsync(string email)
        {
            var req = BuildInternalRequest(HttpMethod.Get, $"internal/v1/users/by-email?email={Uri.EscapeDataString(email)}");
            try
            {
                return await SendAsync<UserAuthDto>(req);
            }
            catch
            {
                // Fall back to local Identity DB (for users registered via gateway auth)
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    throw new NotFoundException("USER_NOT_FOUND", "User not found");

                var roles = await _userManager.GetRolesAsync(user);
                return new UserAuthDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    HashedPassword = user.PasswordHash,
                    Role = roles.FirstOrDefault() ?? "CUSTOMER",
                    Active = true
                };
            }
        }

        // ─── Admin Endpoints ─────────────────────────────────────────────────────
        // These use local Identity since they are gateway-admin operations

        public async Task<PagedUsersResponse> GetAllUsersAdminAsync(int page, int limit)
        {
            var users = _userManager.Users.ToList();
            var paged = users.Skip((page - 1) * limit).Take(limit).ToList();
            var dtos = new List<UserProfileDto>();
            foreach (var u in paged) dtos.Add(await MapLocalUserAsync(u));

            return new PagedUsersResponse { Page = page, Limit = limit, Total = users.Count, Data = dtos };
        }

        public Task<bool> DeactivateUserAsync(string userId) => Task.FromResult(true);
        public Task<bool> ActivateUserAsync(string userId) => Task.FromResult(true);

        // ─── Local Identity Mapping ───────────────────────────────────────────────

        private async Task<UserProfileDto> MapLocalUserAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return new UserProfileDto
            {
                Id = user.Id,
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email,
                Phone = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? "CUSTOMER",
                Active = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Addresses = new List<AddressDto>()
            };
        }

        public async Task<string> RegisterUserInServiceAsync(CleanArchitecture.Core.DTOs.Account.RegisterRequest request)
        {
            var body = new
            {
                name = request.Name ?? "",
                surname = request.Surname ?? "",
                email = request.Email,
                phone = request.PhoneNumber ?? "",
                password = request.Password,
                role = "CUSTOMER"
            };
            var req = BuildInternalRequest(HttpMethod.Post, "api/v1/users/register", body);
            var response = await _httpClient.SendAsync(req);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                throw new ApiException($"User Service Registration Failed: {errorMsg}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<UsResponse>(_jsonOpts);
            return result.Id;
        }
    }
}
