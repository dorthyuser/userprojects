using System.Text.Json.Serialization;

namespace TravelcardFunctionApp.Models
{
    public class Auth
    {
        [JsonPropertyName("oauth2")]
        public OAuth2? OAuth2 { get; set; }
    }
}
