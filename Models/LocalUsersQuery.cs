using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace synctesting1109.Models;

public sealed class LocalUsersQuery
{
    [JsonPropertyName("account_status")]
    public string? AccountStatus { get; set; }

    [RegularExpression(@"^[0-9]+$")]
    [JsonPropertyName("zoho_role_id")]
    public string? ZohoRoleId { get; set; }

    [RegularExpression(@"^[0-9]+$")]
    [JsonPropertyName("zoho_profile_id")]
    public string? ZohoProfileId { get; set; }

    [JsonPropertyName("is_confirmed")]
    public bool? IsConfirmed { get; set; }

    [JsonPropertyName("synced_after")]
    public string? SyncedAfter { get; set; }

    [Range(1, int.MaxValue)]
    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [Range(1, 500)]
    [JsonPropertyName("page_size")]
    public int? PageSize { get; set; }

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; set; }

    [JsonPropertyName("sort_order")]
    public string? SortOrder { get; set; }
}