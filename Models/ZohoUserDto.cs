using System.Text.Json.Serialization;

namespace synctesting1050.Models;

public sealed class ZohoUserDto
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
    [JsonPropertyName("last_name")] public string? LastName { get; set; }
    [JsonPropertyName("full_name")] public string? FullName { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
    [JsonPropertyName("mobile")] public string? Mobile { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("confirm")] public bool? Confirm { get; set; }
    [JsonPropertyName("type__s")] public string? TypeS { get; set; }
    [JsonPropertyName("role")] public ZohoNestedDto? Role { get; set; }
    [JsonPropertyName("profile")] public ZohoNestedDto? Profile { get; set; }
    [JsonPropertyName("reporting_to")] public ZohoNestedDto? ReportingTo { get; set; }
    [JsonPropertyName("country")] public string? Country { get; set; }
    [JsonPropertyName("country_locale")] public string? CountryLocale { get; set; }
    [JsonPropertyName("time_zone")] public string? TimeZone { get; set; }
    [JsonPropertyName("language")] public string? Language { get; set; }
    [JsonPropertyName("created_time")] public string? CreatedTime { get; set; }
    [JsonPropertyName("modified_time")] public string? ModifiedTime { get; set; }
    [JsonPropertyName("created_by")] public ZohoNestedDto? CreatedBy { get; set; }
}
