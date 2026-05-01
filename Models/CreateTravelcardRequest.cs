using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace demo_travelcard_paul.Models;

public class CreateTravelcardRequest
{
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TravelcardTypeEnum TravelcardType { get; set; }

    [Required]
    public DateTimeOffset TravelcardValidFrom { get; set; }

    [Required]
    public DateTimeOffset TravelcardValidTo { get; set; }

    [MaxLength(255)]
    [RegularExpression("^[A-Za-z0-9 ]*$")]
    public string? TravelcardName { get; set; }

    [Required]
    [MinLength(11)]
    [MaxLength(22)]
    [RegularExpression("^[A-Za-z0-9]+$")]
    public string TravelcardNumber { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset TravelcardRequestedDate { get; set; }

    [Required]
    [StringLength(15, MinimumLength = 15)]
    [RegularExpression("^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$")]
    public string TravelcardTransactionReference { get; set; } = string.Empty;

    public DateTimeOffset? TravelcardUsableTo { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(2)]
    public List<CardholderRequest> Cardholders { get; set; } = new();
}