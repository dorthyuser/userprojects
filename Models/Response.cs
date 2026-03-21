using System.Text.Json.Serialization;

namespace Ddctravelcard2026Lambda.Models
{
    public class Response
    {
        [JsonPropertyName("travelcardId")]
        public string TravelcardId { get; set; } = null!;

        [JsonPropertyName("token")]
        public string Token { get; set; } = null!;
    }
}
