using System.Text.Json.Serialization;

namespace TravelcardFunctionApp.Models;

public sealed class CreateTravelcardResponse
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}