using System.Text.Json.Serialization;

namespace DemotravelcardLambda.Models;

public sealed class Response
{
    [JsonPropertyName("travelcardId")]
    public string? TravelcardId { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }
}