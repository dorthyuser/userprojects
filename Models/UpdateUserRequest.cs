using System.Text.Json.Serialization;

namespace zoho_project_csharp.Models
{
    public class UpdateUserRequest
    {
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("role")]
        public RoleRef? Role { get; set; }

        [JsonPropertyName("profile")]
        public ProfileRef? Profile { get; set; }
    }
}
