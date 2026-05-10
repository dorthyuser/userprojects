using System.Text.Json.Serialization;

namespace Dashboard2Lambda.Models;

public sealed class Request
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public RecordStatus? Status { get; set; }
}
