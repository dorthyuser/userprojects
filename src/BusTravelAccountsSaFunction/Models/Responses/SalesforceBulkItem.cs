using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BusTravelAccountsSaFunction;

public sealed class SalesforceBulkItem
{
    [JsonPropertyName("id")]
    public JsonNode? Id { get; set; }

    [JsonPropertyName("successful")]
    public bool Successful { get; set; }

    [JsonPropertyName("payload")]
    public JsonObject Payload { get; set; } = new();
}
