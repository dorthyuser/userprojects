using System.Text.Json.Serialization;

namespace Demo_projectLambda.Models;

public sealed class Response
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}