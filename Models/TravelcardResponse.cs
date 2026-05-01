using System.Text.Json.Serialization;

namespace azuretravelcardapi121.Models;

public sealed class TravelcardResponse
{
    [JsonPropertyName("travelcardId")]
    public Guid TravelcardId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}