using System.Text.Json.Serialization;

namespace BusTravelAccountsSaFunction;

public sealed class ErrorResponse
{
    [JsonPropertyName("error")]
    public ErrorBody Error { get; set; } = new();
}