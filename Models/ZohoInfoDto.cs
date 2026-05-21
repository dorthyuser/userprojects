using System.Text.Json.Serialization;

namespace synctesting1050.Models;

public sealed class ZohoInfoDto
{
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("per_page")] public int PerPage { get; set; }
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("more_records")] public bool MoreRecords { get; set; }
}
