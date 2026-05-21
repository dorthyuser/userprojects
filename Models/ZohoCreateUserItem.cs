using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace synctesting1109.Models;

public sealed class ZohoCreateUserItem
{
    [MaxLength(100)]
    [RegularExpression(@"^[A-Za-z .'\-]+$")]
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    [RegularExpression(@"^[A-Za-z .'\-]+$")]
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [Required]
    [MaxLength(254)]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [MaxLength(30)]
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [MaxLength(30)]
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [Required]
    [RegularExpression(@"^[0-9]+$")]
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [Required]
    [RegularExpression(@"^[0-9]+$")]
    [JsonPropertyName("profile")]
    public string? Profile { get; set; }

    [MaxLength(10)]
    [JsonPropertyName("country_locale")]
    public string? CountryLocale { get; set; }

    [MaxLength(60)]
    [JsonPropertyName("time_zone")]
    public string? TimeZone { get; set; }
}