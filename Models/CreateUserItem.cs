using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace synctesting1050.Models;

public sealed class CreateUserItem
{
    [JsonPropertyName("first_name")]
    [StringLength(100, MinimumLength = 0)]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    [Required]
    [StringLength(254, MinimumLength = 1)]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    [StringLength(30)]
    public string? Phone { get; set; }

    [JsonPropertyName("mobile")]
    [StringLength(30)]
    public string? Mobile { get; set; }

    [JsonPropertyName("role")]
    [Required]
    [RegularExpression("^[0-9]+$")]
    public string? Role { get; set; }

    [JsonPropertyName("profile")]
    [Required]
    [RegularExpression("^[0-9]+$")]
    public string? Profile { get; set; }

    [JsonPropertyName("country_locale")]
    [StringLength(10)]
    public string? CountryLocale { get; set; }

    [JsonPropertyName("time_zone")]
    [StringLength(60)]
    public string? TimeZone { get; set; }
}
