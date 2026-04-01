using System.Text.Json.Serialization;

namespace TravelcardFunctionApp.Models
{
    public class OAuth2
    {
        [JsonPropertyName("clientId")]
        public string? ClientId { get; set; }

        [JsonPropertyName("clientSecret")]
        public string? ClientSecret { get; set; }

        [JsonPropertyName("tokenUrl")]
        public string? TokenUrl { get; set; }

        [JsonPropertyName("grantType")]
        public GrantType? GrantType { get; set; }

        [JsonPropertyName("authorizationUrl")]
        public string? AuthorizationUrl { get; set; }

        [JsonPropertyName("scopes")]
        public string[]? Scopes { get; set; }
    }
}
