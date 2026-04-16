using System;
using System.Text.Json.Serialization;

namespace hello_http_test.Models
{
    /// <summary>
    /// Represents OAuth token response from Zoho.
    /// </summary>
    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// The moment the token was obtained (UTC).
        /// </summary>
        public DateTime ObtainedAtUtc { get; set; }

        /// <summary>
        /// Helper to determine expiry.
        /// </summary>
        public bool IsExpired()
        {
            if (ExpiresIn <= 0) return true;
            return DateTime.UtcNow >= ObtainedAtUtc.AddSeconds(ExpiresIn - 60); // refresh 60s early
        }
    }
}
