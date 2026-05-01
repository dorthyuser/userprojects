using System.ComponentModel.DataAnnotations;

namespace azuretravelcardapi907.Models;

public class CreateTravelcardRequest
{
    [Required]
    public TravelcardTypeEnum TravelcardType { get; set; }

    [Required]
    public DateTimeOffset TravelcardValidFrom { get; set; }

    [Required]
    public DateTimeOffset TravelcardValidTo { get; set; }

    [MaxLength(255)]
    public string? TravelcardName { get; set; }

    [Required]
    [MinLength(11)]
    [MaxLength(22)]
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