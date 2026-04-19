using System.Text.Json.Serialization;

namespace ZohoProject1.Models
{
    public class ZohoTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("api_domain")]
        public string? ApiDomain { get; set; }
    }
}
