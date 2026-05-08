using System.Text.Json.Serialization;

namespace Travelcardlambdacsharp1105Lambda.Models;

public sealed class CreateTravelcardResponse
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = string.Empty;
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}