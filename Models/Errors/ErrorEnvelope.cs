using System.Text.Json.Serialization;

namespace BusTravelAccountsSaMainLambda;

public sealed class ErrorEnvelope
{
    [JsonPropertyName("error")]
    public ErrorDetail? Error { get; set; }
}