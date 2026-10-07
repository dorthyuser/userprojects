using System.Text.Json.Serialization;

namespace MulecombineMainLambda;

public sealed class SaHealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}