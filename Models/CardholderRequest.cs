using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace demo_travelcard_paul.Models;

public class CardholderRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(15)]
    public string CardholderTitle { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string CardholderForename { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string CardholderSurname { get; set; } = string.Empty;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CardholderTypeEnum CardholderType { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string CardholderPhotoName { get; set; } = string.Empty;

    [StringLength(42, MinimumLength = 39)]
    public string? CardholderPhotoRRSKey { get; set; }

    [StringLength(2048, MinimumLength = 20)]
    [Url]
    public string? CardholderPhotoURL { get; set; }

    [StringLength(42, MinimumLength = 39)]
    public string? CardholderPhotoKey { get; set; }
}