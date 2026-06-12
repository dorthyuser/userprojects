using System.Text.Json.Serialization;

namespace TravelCardFunctionApp.Models;

public class CreateTravelCardResponse
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
