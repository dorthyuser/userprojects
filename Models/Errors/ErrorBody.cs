using System.Text.Json.Serialization;

namespace BusTravelAccountsLambda;

public sealed class ErrorBody
{
    [JsonPropertyName("error")]
    public ErrorDetail Error { get; set; } = new();
}
