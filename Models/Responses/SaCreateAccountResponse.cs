using System.Text.Json.Serialization;

namespace MulecombineMainLambda;

public sealed class SaCreateAccountResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}