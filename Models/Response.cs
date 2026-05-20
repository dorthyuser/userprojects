using System.Text.Json.Serialization;

namespace DemoshauntcLambda.Models;

public sealed class Response
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = null!;

    [JsonPropertyName("token")]
    public string Token { get; set; } = null!;
}