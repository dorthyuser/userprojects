using System.Text.Json.Serialization;

namespace travelcardservice.Models
{
    public class CreateResponse
    {
        [JsonPropertyName("travelcardId")]
        public string TravelcardId { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }
}
