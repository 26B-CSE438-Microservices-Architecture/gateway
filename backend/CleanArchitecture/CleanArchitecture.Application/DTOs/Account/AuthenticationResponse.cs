using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Account
{
    public class AuthenticationResponse
    {
        [JsonPropertyName("access_token")]
        public string JWToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 3600;

        [JsonPropertyName("user")]
        public UserDto User { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }
    }

    public class RegisterResponse
    {
        public string Message { get; set; }
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }
    }

    public class RefreshTokenRequest
    {
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }

    public class RefreshTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public class LogoutRequest
    {
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }

    public class ChangePasswordRequest
    {
        [JsonPropertyName("old_password")]
        public string OldPassword { get; set; }
        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }
    }

    public class DeleteAccountRequest
    {
        public string Password { get; set; }
    }

    public class VerifyTokenRequest
    {
        public string Token { get; set; }
    }
}
