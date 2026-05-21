using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace synctesting1109.Models;

public sealed class ZohoGetUsersQuery
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [Range(1, int.MaxValue)]
    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [Range(1, 200)]
    [JsonPropertyName("per_page")]
    public int? PerPage { get; set; }

    [JsonPropertyName("if_modified_since")]
    public string? IfModifiedSince { get; set; }
}