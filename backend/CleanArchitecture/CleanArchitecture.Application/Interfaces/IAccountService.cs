using CleanArchitecture.Core.DTOs.Account;
using CleanArchitecture.Core.DTOs.Email;
using CleanArchitecture.Core.Wrappers;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IAccountService
    {
        Task<AuthenticationResponse> LoginAsync(AuthenticationRequest request, string ipAddress);
        Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress);
        Task<bool> LogoutAsync(LogoutRequest request, string ipAddress);
        Task<RegisterResponse> RegisterAsync(RegisterRequest request, string origin);
        Task<string> ConfirmEmailAsync(string userId, string code);
        Task<EmailRequest> ForgotPassword(ForgotPasswordRequest model, string origin);
        Task<string> ResetPassword(ResetPasswordRequest model);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);
        Task<bool> UpdateProfileAsync(string userId, UpdateProfileRequest request);
        Task<bool> DeleteAccountAsync(string userId, DeleteAccountRequest request);
        Task AddToRoleAsync(string userId, string roleName);
    }
}
