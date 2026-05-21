using System.Text.Json.Serialization;

namespace synctesting1050.Models;

public sealed class ZohoNestedDto
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
}
