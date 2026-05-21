using System.Text.Json.Serialization;

namespace synctesting1050.Models;

public sealed class LocalUserDto
{
    [JsonPropertyName("user_pk")] public long UserPk { get; set; }
    [JsonPropertyName("zoho_uid")] public string? ZohoUid { get; set; }
    [JsonPropertyName("given_name")] public string? GivenName { get; set; }
    [JsonPropertyName("family_name")] public string? FamilyName { get; set; }
    [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
    [JsonPropertyName("email_address")] public string? EmailAddress { get; set; }
    [JsonPropertyName("phone_number")] public string? PhoneNumber { get; set; }
    [JsonPropertyName("mobile_number")] public string? MobileNumber { get; set; }
    [JsonPropertyName("account_status")] public string? AccountStatus { get; set; }
    [JsonPropertyName("is_confirmed")] public bool? IsConfirmed { get; set; }
    [JsonPropertyName("user_type")] public string? UserType { get; set; }
    [JsonPropertyName("zoho_role_id")] public string? ZohoRoleId { get; set; }
    [JsonPropertyName("zoho_role_name")] public string? ZohoRoleName { get; set; }
    [JsonPropertyName("zoho_profile_id")] public string? ZohoProfileId { get; set; }
    [JsonPropertyName("zoho_profile_name")] public string? ZohoProfileName { get; set; }
    [JsonPropertyName("reports_to_uid")] public string? ReportsToUid { get; set; }
    [JsonPropertyName("country_code")] public string? CountryCode { get; set; }
    [JsonPropertyName("locale_code")] public string? LocaleCode { get; set; }
    [JsonPropertyName("iana_timezone")] public string? IanaTimezone { get; set; }
    [JsonPropertyName("zoho_created_at")] public DateTimeOffset? ZohoCreatedAt { get; set; }
    [JsonPropertyName("zoho_modified_at")] public DateTimeOffset? ZohoModifiedAt { get; set; }
    [JsonPropertyName("local_synced_at")] public DateTimeOffset? LocalSyncedAt { get; set; }
}
