using System.Text.Json.Serialization;

namespace BusTravelAccountsSaFunction;

public sealed class ErrorEnvelope
{
    [JsonPropertyName("error")]
    public ErrorBody Error { get; set; } = new();
}