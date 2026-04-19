using System.Text.Json.Serialization;

namespace ZohoProject2.Models
{
    public class ZohoTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        // ✅ ADD THIS
        [JsonPropertyName("api_domain")]
        public string? ApiDomain { get; set; }
    }
}
