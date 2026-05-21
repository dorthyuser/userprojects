using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace synctesting1109.Models;

public sealed class ZohoSyncUsersRequest
{
    [JsonPropertyName("full_sync")]
    public bool? FullSync { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [Range(1, 200)]
    [JsonPropertyName("per_page")]
    public int? PerPage { get; set; }
}