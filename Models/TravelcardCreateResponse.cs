using System.Text.Json.Serialization;

namespace azuretravelcardapi1218.Models;

public sealed class TravelcardCreateResponse
{
    [JsonPropertyName("travelcardId")]
    public Guid TravelcardId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}