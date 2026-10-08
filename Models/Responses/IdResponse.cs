using System.Text.Json.Serialization;

namespace BusTravelAccountsSaMainLambda;

public sealed class IdResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}