using CleanArchitecture.Core.DTOs.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileDto> GetProfileAsync(string userId);
        Task RegisterDeviceAsync(string userId, RegisterDeviceRequest request);
        Task<List<AddressDto>> GetAddressesAsync(string userId);
        Task<AddressDto> CreateAddressAsync(string userId, CreateAddressRequest request);
        Task<AddressDto> UpdateAddressAsync(string userId, string addressId, UpdateAddressRequest request);
        Task DeleteAddressAsync(string userId, string addressId);
        Task<PagedFavoritesResponse> GetFavoritesAsync(string userId, int page, int limit);
        Task AddFavoriteAsync(string userId, string vendorId);
        Task RemoveFavoriteAsync(string userId, string vendorId);
    }
}
