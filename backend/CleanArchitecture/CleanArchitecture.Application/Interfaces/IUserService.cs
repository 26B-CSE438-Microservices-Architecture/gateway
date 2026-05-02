using CleanArchitecture.Core.DTOs.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IUserService
    {
        // Public Endpoints
        Task<UserProfileDto> GetProfileAsync(string userId);
        Task<UserProfileDto> UpdateProfileAsync(string userId, UserUpdateProfileRequest request);
        
        // Address Management
        Task<List<AddressDto>> GetAddressesAsync(string userId);
        Task<AddressDto> CreateAddressAsync(string userId, CreateAddressRequest request);
        Task DeleteAddressAsync(string userId, string addressId);
        
        // Favorites (Inner Contract)
        Task<PagedFavoritesResponse<UserStoreFavorite>> GetFavoritesAsync(string userId, int page, int limit);
        Task AddFavoriteAsync(string userId, string vendorId);
        Task RemoveFavoriteAsync(string userId, string vendorId);
        
        // Internal Endpoints
        Task<UserProfileDto> GetInternalUserByIdAsync(string userId);
        Task<List<UserProfileDto>> LookupUsersAsync(List<string> userIds);
        Task<UserAuthDto> GetUserByEmailAsync(string email);
        Task<string> RegisterUserInServiceAsync(CleanArchitecture.Core.DTOs.Account.RegisterRequest request);
        
        // Admin Endpoints
        Task<PagedUsersResponse> GetAllUsersAdminAsync(int page, int limit);
        Task<bool> DeactivateUserAsync(string userId);
        Task<bool> ActivateUserAsync(string userId);
    }
}
