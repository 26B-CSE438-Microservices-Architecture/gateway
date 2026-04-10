using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.User
{
    public class RegisterDeviceRequest
    {
        [JsonPropertyName("device_token")]
        public string DeviceToken { get; set; }

        [JsonPropertyName("platform")]
        public string Platform { get; set; }
    }
}
