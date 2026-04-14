using System.Text.Json.Serialization;

namespace tc_testing_api2.Models
{
    /// <summary>
    /// Model representing the OAuth2 token response.
    /// </summary>
    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
    }
}