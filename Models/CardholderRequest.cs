using System.ComponentModel.DataAnnotations;

namespace azuretravelcardapi907.Models;

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

    [MinLength(39)]
    [MaxLength(42)]
    public string? CardholderPhotoRRSKey { get; set; }

    [MinLength(20)]
    [MaxLength(2048)]
    public string? CardholderPhotoURL { get; set; }

    [MinLength(39)]
    [MaxLength(42)]
    public string? CardholderPhotoKey { get; set; }
}