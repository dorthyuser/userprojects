using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BusTravelAccountsSaFunction;

public sealed class SalesforceBulkResult
{
    [JsonPropertyName("successful")]
    public bool Successful { get; set; }

    [JsonPropertyName("items")]
    public List<SalesforceBulkItem> Items { get; set; } = [];
}
