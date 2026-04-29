using System.Text.Json.Serialization;

namespace Models
{
    public class TravelcardResponse
    {
        [JsonPropertyName("travelcardId")]
        public string TravelcardId { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }
}