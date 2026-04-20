using CleanArchitecture.Core.DTOs.User;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        // Mock in-memory stores for addresses and favorites
        private static readonly Dictionary<string, List<AddressDto>> _addresses = new();
        private static readonly Dictionary<string, List<UserStoreFavorite>> _favorites = new();

        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserProfileDto> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("USER_NOT_FOUND", "User not found");

            return await MapToProfileAsync(user);
        }

        public async Task<UserProfileDto> UpdateProfileAsync(string userId, UserUpdateProfileRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("USER_NOT_FOUND", "User not found");

            if (request.Name != null) user.FirstName = request.Name;
            if (request.Phone != null) user.PhoneNumber = request.Phone;

            await _userManager.UpdateAsync(user);
            return await MapToProfileAsync(user);
        }

        public async Task<List<AddressDto>> GetAddressesAsync(string userId)
        {
            await Task.Yield();
            if (!_addresses.ContainsKey(userId))
            {
                _addresses[userId] = new List<AddressDto>
                {
                    new AddressDto
                    {
                        Id = "addr_1",
                        Label = "Home",
                        City = "Antalya",
                        Street = "Ataturk Cad. No:5",
                        PostalCode = "07100",
                        Lat = 36.8969,
                        Lng = 30.7133
                    }
                };
            }

            return _addresses[userId];
        }

        public Task<AddressDto> CreateAddressAsync(string userId, CreateAddressRequest request)
        {
            if (!_addresses.ContainsKey(userId))
                _addresses[userId] = new List<AddressDto>();

            var newAddress = new AddressDto
            {
                Id = $"addr_{Guid.NewGuid():N}".Substring(0, 10),
                Label = request.Label,
                City = request.City,
                Street = request.Street,
                PostalCode = request.PostalCode,
                Lat = request.Lat,
                Lng = request.Lng
            };

            _addresses[userId].Add(newAddress);
            return Task.FromResult(newAddress);
        }

        public Task DeleteAddressAsync(string userId, string addressId)
        {
            if (!_addresses.ContainsKey(userId))
                throw new NotFoundException("ADDRESS_NOT_FOUND", "Address not found");

            var address = _addresses[userId].FirstOrDefault(a => a.Id == addressId);
            if (address == null)
                throw new NotFoundException("ADDRESS_NOT_FOUND", "Address not found");

            _addresses[userId].Remove(address);
            return Task.CompletedTask;
        }

        // --- Favorites Implementation ---

        public Task<PagedFavoritesResponse<UserStoreFavorite>> GetFavoritesAsync(string userId, int page, int limit)
        {
            if (!_favorites.ContainsKey(userId))
            {
                _favorites[userId] = new List<UserStoreFavorite>
                {
                    new UserStoreFavorite { VendorId = "vendor_101", CreatedAt = DateTime.UtcNow.AddHours(-1) }
                };
            }

            var all = _favorites[userId].OrderByDescending(f => f.CreatedAt).ToList();
            var paged = all.Skip((page - 1) * limit).Take(limit).ToList();

            return Task.FromResult(new PagedFavoritesResponse<UserStoreFavorite>
            {
                Page = page,
                Limit = limit,
                Total = all.Count,
                Data = paged
            });
        }

        public Task AddFavoriteAsync(string userId, string vendorId)
        {
            if (!_favorites.ContainsKey(userId))
                _favorites[userId] = new List<UserStoreFavorite>();

            if (!_favorites[userId].Any(f => f.VendorId == vendorId))
            {
                _favorites[userId].Add(new UserStoreFavorite
                {
                    VendorId = vendorId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return Task.CompletedTask;
        }

        public Task RemoveFavoriteAsync(string userId, string vendorId)
        {
            if (!_favorites.ContainsKey(userId))
                return Task.CompletedTask;

            var fav = _favorites[userId].FirstOrDefault(f => f.VendorId == vendorId);
            if (fav != null)
            {
                _favorites[userId].Remove(fav);
            }

            return Task.CompletedTask;
        }

        // Internal Endpoints
        public async Task<UserProfileDto> GetInternalUserByIdAsync(string userId)
        {
            return await GetProfileAsync(userId);
        }

        public async Task<List<UserProfileDto>> LookupUsersAsync(List<string> userIds)
        {
            var result = new List<UserProfileDto>();
            foreach (var id in userIds)
            {
                try { result.Add(await GetProfileAsync(id)); } catch { }
            }
            return result;
        }

        public async Task<UserAuthDto> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new NotFoundException("USER_NOT_FOUND", "User not found");

            var roles = await _userManager.GetRolesAsync(user);

            return new UserAuthDto
            {
                Id = user.Id,
                Email = user.Email,
                HashedPassword = "$2b$12$K8M... (Mock BCrypt)",
                Role = roles.FirstOrDefault() ?? "CUSTOMER",
                Active = true
            };
        }

        public async Task<PagedUsersResponse> GetAllUsersAdminAsync(int page, int limit)
        {
            var users = _userManager.Users.ToList();
            var paged = users.Skip((page - 1) * limit).Take(limit).ToList();
            
            var dtos = new List<UserProfileDto>();
            foreach(var u in paged) dtos.Add(await MapToProfileAsync(u));

            return new PagedUsersResponse
            {
                Page = page,
                Limit = limit,
                Total = users.Count,
                Data = dtos
            };
        }

        public Task<bool> DeactivateUserAsync(string userId) => Task.FromResult(true);
        public Task<bool> ActivateUserAsync(string userId) => Task.FromResult(true);

        private async Task<UserProfileDto> MapToProfileAsync(ApplicationUser user)
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
                Addresses = await GetAddressesAsync(user.Id)
            };
        }
    }
}
