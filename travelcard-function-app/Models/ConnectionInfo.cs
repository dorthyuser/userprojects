using System.Text.Json.Serialization;

namespace TravelcardFunctionApp.Models
{
    public class Connection
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("protocol")]
        public Protocol? Protocol { get; set; }

        [JsonPropertyName("authMethod")]
        public AuthMethod? AuthMethod { get; set; }
    }
}
