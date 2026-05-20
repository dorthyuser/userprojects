using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace zohotesting.Models;

public sealed class CreateZohoUserRequest
{
    [JsonPropertyName("users")]
    [Required]
    public List<CreateZohoUserItem> Users { get; set; } = new();
}

public sealed class CreateZohoUserItem
{
    [JsonPropertyName("first_name")]
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    [Required]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [JsonPropertyName("mobile")]
    [MaxLength(30)]
    public string? Mobile { get; set; }

    [JsonPropertyName("role")]
    [Required]
    [RegularExpression("^[0-9]+$")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("profile")]
    [Required]
    [RegularExpression("^[0-9]+$")]
    public string Profile { get; set; } = string.Empty;

    [JsonPropertyName("country_locale")]
    [MaxLength(10)]
    public string? CountryLocale { get; set; }

    [JsonPropertyName("time_zone")]
    [MaxLength(60)]
    public string? TimeZone { get; set; }
}

public sealed class SyncZohoUsersRequest
{
    [JsonPropertyName("full_sync")]
    public bool? FullSync { get; set; }

    [JsonPropertyName("type")]
    [MaxLength(20)]
    public string? Type { get; set; }

    [JsonPropertyName("per_page")]
    [Range(1, 200)]
    public int? PerPage { get; set; }
}

public sealed class LocalUsersQuery
{
    [JsonPropertyName("account_status")]
    public string? AccountStatus { get; set; }

    [JsonPropertyName("zoho_role_id")]
    public string? ZohoRoleId { get; set; }

    [JsonPropertyName("zoho_profile_id")]
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
    public string? SortBy { get; set; }

    [JsonPropertyName("sort_order")]
    public string? SortOrder { get; set; }
}

public sealed class ServiceResult
{
    public ServiceResult(int statusCode, string body, string? correlationId)
    {
        StatusCode = statusCode;
        Body = body;
        CorrelationId = correlationId;
    }

    public int StatusCode { get; }
    public string Body { get; }
    public string? CorrelationId { get; }
}