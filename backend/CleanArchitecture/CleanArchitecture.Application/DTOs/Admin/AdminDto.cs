using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Admin
{
    public class AdminUserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        public List<string> Roles { get; set; }

        [JsonPropertyName("email_confirmed")]
        public bool EmailConfirmed { get; set; }
    }

    public class PagedUsersResponse
    {
        public List<AdminUserDto> Users { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        public int Page { get; set; }
        public int Limit { get; set; }
    }

    public class AssignRoleRequest
    {
        public string Role { get; set; }
    }

    public class AssignRoleResponse
    {
        public string Message { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("assigned_role")]
        public string AssignedRole { get; set; }
    }
}
