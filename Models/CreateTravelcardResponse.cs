using System.Text.Json.Serialization;

namespace demo_travelcard_paul.Models;

public class CreateTravelcardResponse
{
    [JsonPropertyName("travelcardId")]
    public int TravelcardId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}