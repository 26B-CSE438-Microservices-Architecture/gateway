using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.User
{
    public class UserProfileDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("surname")]
        public string Surname { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("loyalty_points")]
        public int LoyaltyPoints { get; set; }

        [JsonPropertyName("notification_preferences")]
        public NotificationPreferencesDto NotificationPreferences { get; set; }
    }

    public class NotificationPreferencesDto
    {
        [JsonPropertyName("push_enabled")]
        public bool PushEnabled { get; set; }

        [JsonPropertyName("sms_enabled")]
        public bool SmsEnabled { get; set; }

        [JsonPropertyName("email_enabled")]
        public bool EmailEnabled { get; set; }
    }
}
