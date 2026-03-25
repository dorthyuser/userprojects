using System.Text.Json.Serialization;

namespace TravelcardFunction.Models
{
    public class CreateTravelcardResponse
    {
        [JsonPropertyName("travelcardId")]
        public string TravelcardId { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }
}
