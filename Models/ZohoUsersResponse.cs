using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace ZohoCrmOauthFinal.Models
{
    public class ZohoUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    public class ZohoUsersResponse
    {
        [JsonPropertyName("users")]
        public List<ZohoUser>? Users { get; set; }

        [JsonPropertyName("info")]
        public object? Info { get; set; }
    }
}
