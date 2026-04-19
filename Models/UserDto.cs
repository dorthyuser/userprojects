using System.Text.Json.Serialization;

namespace ZohoProject2.Models
{
    public class UserDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}
