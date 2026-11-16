using System.Text.Json.Serialization;

namespace BusTravelAccountsSaFunction;

public sealed class AccountIdResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
