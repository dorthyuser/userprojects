using System.ComponentModel.DataAnnotations;

namespace demo_travelcard_aus.Models;

public class TravelcardCreateRequest
{
    [Required]
    public TravelcardTypeEnum TravelcardType { get; set; }

    [Required]
    public DateTimeOffset TravelcardValidFrom { get; set; }

    [Required]
    public DateTimeOffset TravelcardValidTo { get; set; }

    [MaxLength(255)]
    [RegularExpression(@"^[A-Za-z0-9 ]*$")]
    public string? TravelcardName { get; set; }

    [Required]
    [MinLength(11)]
    [MaxLength(22)]
    [RegularExpression(@"^[A-Za-z0-9]+$")]
    public string TravelcardNumber { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset TravelcardRequestedDate { get; set; }

    [Required]
    [StringLength(15, MinimumLength = 15)]
    public string TravelcardTransactionReference { get; set; } = string.Empty;

    public DateTimeOffset? TravelcardUsableTo { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one cardholder is required")]
    public List<CardholderRequest> Cardholders { get; set; } = new();
}