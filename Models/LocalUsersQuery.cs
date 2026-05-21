using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace synctesting1050.Models;

public sealed class LocalUsersQuery
{
    [JsonPropertyName("account_status")]
    [RegularExpression("^(active|inactive)$")]
    public string? AccountStatus { get; set; }

    [JsonPropertyName("zoho_role_id")]
    [RegularExpression("^[0-9]+$")]
    public string? ZohoRoleId { get; set; }

    [JsonPropertyName("zoho_profile_id")]
    [RegularExpression("^[0-9]+$")]
    public string? ZohoProfileId { get; set; }

    [JsonPropertyName("is_confirmed")]
    public bool? IsConfirmed { get; set; }

    [JsonPropertyName("synced_after")]
    public DateTimeOffset? SyncedAfter { get; set; }

    [JsonPropertyName("page")]
    [Range(1, int.MaxValue)]
    public int? Page { get; set; }

    [JsonPropertyName("page_size")]
    [Range(1, 500)]
    public int? PageSize { get; set; }

    [JsonPropertyName("sort_by")]
    [RegularExpression("^(family_name|email_address|zoho_modified_at|local_synced_at)$")]
    public string? SortBy { get; set; }

    [JsonPropertyName("sort_order")]
    [RegularExpression("^(asc|desc)$")]
    public string? SortOrder { get; set; }
}
