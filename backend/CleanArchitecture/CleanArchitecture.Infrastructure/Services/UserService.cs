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
        private static readonly Dictionary<string, List<FavoriteVendorDto>> _favorites = new();

        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserProfileDto> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("USER_NOT_FOUND", "User not found");

            return new UserProfileDto
            {
                Id = user.Id,
                Name = user.FirstName,
                Surname = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                LoyaltyPoints = 420,
                NotificationPreferences = new NotificationPreferencesDto
                {
                    PushEnabled = true,
                    SmsEnabled = false,
                    EmailEnabled = true
                }
            };
        }

        public Task RegisterDeviceAsync(string userId, RegisterDeviceRequest request)
        {
            // Mock: device token stored (no-op for now)
            return Task.CompletedTask;
        }

        public Task<List<AddressDto>> GetAddressesAsync(string userId)
        {
            if (!_addresses.ContainsKey(userId))
            {
                _addresses[userId] = new List<AddressDto>
                {
                    new AddressDto
                    {
                        Id = "addr_1",
                        AddressTitle = "Ev",
                        City = "Antalya",
                        District = "Kepez",
                        Neighborhood = "Kültür Mah",
                        Street = "3818 Sokak",
                        BuildingNo = "8",
                        Floor = "3",
                        ApartmentNo = "6",
                        AddressDescription = "Kapý zili çalýþmýyor",
                        Phone = "05555555555",
                        Location = new LocationDto { Lat = 36.884804, Lng = 30.704044 },
                        MaskedPhone = "555*****55",
                        ShowsMapPreview = true,
                        IsCurrent = true
                    }
                };
            }

            return Task.FromResult(_addresses[userId]);
        }

        public Task<AddressDto> CreateAddressAsync(string userId, CreateAddressRequest request)
        {
            if (!_addresses.ContainsKey(userId))
                _addresses[userId] = new List<AddressDto>();

            var phone = request.Phone ?? "";
            var maskedPhone = phone.Length >= 10
                ? phone.Substring(0, 3) + "*****" + phone.Substring(phone.Length - 2)
                : phone;

            var newAddress = new AddressDto
            {
                Id = $"addr_{Guid.NewGuid():N}".Substring(0, 10),
                AddressTitle = request.AddressTitle,
                City = request.City,
                District = request.District,
                Neighborhood = request.Neighborhood,
                Street = request.Street,
                BuildingNo = request.BuildingNo,
                Floor = request.Floor,
                ApartmentNo = request.ApartmentNo,
                AddressDescription = request.AddressDescription,
                Phone = request.Phone,
                Location = request.Location,
                MaskedPhone = maskedPhone,
                ShowsMapPreview = request.Location != null,
                IsCurrent = false
            };

            _addresses[userId].Add(newAddress);
            return Task.FromResult(newAddress);
        }

        public Task<AddressDto> UpdateAddressAsync(string userId, string addressId, UpdateAddressRequest request)
        {
            if (!_addresses.ContainsKey(userId))
                throw new NotFoundException("ADDRESS_NOT_FOUND", "Address not found");

            var address = _addresses[userId].FirstOrDefault(a => a.Id == addressId);
            if (address == null)
                throw new NotFoundException("ADDRESS_NOT_FOUND", "Address not found");

            if (request.AddressTitle != null) address.AddressTitle = request.AddressTitle;
            if (request.City != null) address.City = request.City;
            if (request.District != null) address.District = request.District;
            if (request.Neighborhood != null) address.Neighborhood = request.Neighborhood;
            if (request.Street != null) address.Street = request.Street;
            if (request.BuildingNo != null) address.BuildingNo = request.BuildingNo;
            if (request.Floor != null) address.Floor = request.Floor;
            if (request.ApartmentNo != null) address.ApartmentNo = request.ApartmentNo;
            if (request.AddressDescription != null) address.AddressDescription = request.AddressDescription;
            if (request.Phone != null) address.Phone = request.Phone;
            if (request.Location != null) address.Location = request.Location;

            return Task.FromResult(address);
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

        public Task<PagedFavoritesResponse> GetFavoritesAsync(string userId, int page, int limit)
        {
            if (!_favorites.ContainsKey(userId))
            {
                _favorites[userId] = new List<FavoriteVendorDto>
                {
                    new FavoriteVendorDto
                    {
                        VendorId = "vendor_101",
                        Name = "Burger Point",
                        ImageUrl = "https://cdn.app.com/burger.jpg"
                    }
                };
            }

            var all = _favorites[userId];
            var paged = all.Skip((page - 1) * limit).Take(limit).ToList();

            return Task.FromResult(new PagedFavoritesResponse
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
                _favorites[userId] = new List<FavoriteVendorDto>();

            if (!_favorites[userId].Any(f => f.VendorId == vendorId))
            {
                _favorites[userId].Add(new FavoriteVendorDto
                {
                    VendorId = vendorId,
                    Name = $"Vendor {vendorId}",
                    ImageUrl = $"https://cdn.app.com/{vendorId}.jpg"
                });
            }

            return Task.CompletedTask;
        }

        public Task RemoveFavoriteAsync(string userId, string vendorId)
        {
            if (!_favorites.ContainsKey(userId))
                throw new NotFoundException("FAVORITE_NOT_FOUND", "Favorite not found");

            var fav = _favorites[userId].FirstOrDefault(f => f.VendorId == vendorId);
            if (fav == null)
                throw new NotFoundException("FAVORITE_NOT_FOUND", "Favorite not found");

            _favorites[userId].Remove(fav);
            return Task.CompletedTask;
        }
    }
}
