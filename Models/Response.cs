using System.Text.Json.Serialization;

namespace Travelcardcsharplambda355Lambda.Models;

public sealed class Response
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = null!;

    [JsonPropertyName("token")]
    public string Token { get; set; } = null!;
}
