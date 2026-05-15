using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Travelcarddemo1251Lambda.Models;

public class CreateTravelcardRequest
{
    [Required]
    public TravelcardTypeEnum TravelcardType { get; set; }

    [Required]
    public DateTimeOffset TravelcardValidFrom { get; set; }

    [Required]
    public DateTimeOffset TravelcardValidTo { get; set; }

    [StringLength(255)]
    [RegularExpression(@"^[A-Za-z0-9 ]*$")]
    public string? TravelcardName { get; set; }

    [Required]
    [StringLength(22, MinimumLength = 11)]
    [RegularExpression(@"^[A-Za-z0-9]+$")]
    public string TravelcardNumber { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset TravelcardRequestedDate { get; set; }

    [Required]
    [StringLength(15, MinimumLength = 15)]
    public string TravelcardTransactionReference { get; set; } = string.Empty;

    public DateTimeOffset? TravelcardUsableTo { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(2)]
    public List<CardholderRequest> Cardholders { get; set; } = new();
}

public class CardholderRequest
{
    [Required]
    [StringLength(15, MinimumLength = 1)]
    public string CardholderTitle { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CardholderForename { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CardholderSurname { get; set; } = string.Empty;

    [Required]
    public CardholderTypeEnum CardholderType { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CardholderPhotoName { get; set; } = string.Empty;

    [StringLength(42, MinimumLength = 39)]
    public string? CardholderPhotoRRSKey { get; set; }

    [StringLength(2048, MinimumLength = 20)]
    public string? CardholderPhotoURL { get; set; }

    [StringLength(42, MinimumLength = 39)]
    public string? CardholderPhotoKey { get; set; }
}