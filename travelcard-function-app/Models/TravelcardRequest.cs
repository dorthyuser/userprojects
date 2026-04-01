using System.Text.Json.Serialization;

namespace TravelcardFunctionApp.Models
{
    public class TravelcardRequest
    {
        [JsonPropertyName("connection")]
        public Connection? Connection { get; set; }

        [JsonPropertyName("auth")]
        public Auth? Auth { get; set; }
    }
}
