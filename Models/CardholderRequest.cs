using System.ComponentModel.DataAnnotations;

namespace demo_travelcard_aus.Models;

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
    public CardholderTypeEnum CardholderType { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string CardholderPhotoName { get; set; } = string.Empty;

    [MaxLength(42)]
    public string? CardholderPhotoRRSKey { get; set; }

    [MaxLength(2048)]
    [Url]
    public string? CardholderPhotoURL { get; set; }

    [MaxLength(42)]
    public string? CardholderPhotoKey { get; set; }
}