using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace synctesting1050.Models;

public sealed class SyncUsersRequest
{
    [JsonPropertyName("full_sync")]
    public bool? FullSync { get; set; }

    [JsonPropertyName("type")]
    [RegularExpression("^(AllUsers|ActiveUsers|DeactiveUsers)$")]
    public string? Type { get; set; }

    [JsonPropertyName("per_page")]
    [Range(1, 200)]
    public int? PerPage { get; set; }
}
