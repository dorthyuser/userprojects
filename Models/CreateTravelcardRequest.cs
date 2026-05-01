using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace new_test_for_demo.Models;

public class CreateTravelcardRequest : IValidatableObject
{
    [Required]
    [JsonPropertyName("travelcardType")]
    public TravelcardTypeEnum TravelcardType { get; set; }

    [Required]
    [JsonPropertyName("travelcardValidFrom")]
    public DateTimeOffset TravelcardValidFrom { get; set; }

    [Required]
    [JsonPropertyName("travelcardValidTo")]
    public DateTimeOffset TravelcardValidTo { get; set; }

    [StringLength(255)]
    [RegularExpression(@"^[A-Za-z0-9 ]*$")]
    [JsonPropertyName("travelcardName")]
    public string? TravelcardName { get; set; }

    [Required]
    [MinLength(11)]
    [MaxLength(22)]
    [RegularExpression(@"^[A-Za-z0-9]+$")]
    [JsonPropertyName("travelcardNumber")]
    public string TravelcardNumber { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("travelcardRequestedDate")]
    public DateTimeOffset TravelcardRequestedDate { get; set; }

    [Required]
    [StringLength(15, MinimumLength = 15)]
    [RegularExpression(@"^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$")]
    [JsonPropertyName("travelcardTransactionReference")]
    public string TravelcardTransactionReference { get; set; } = string.Empty;

    [JsonPropertyName("travelcardUsableTo")]
    public DateTimeOffset? TravelcardUsableTo { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(2)]
    [JsonPropertyName("cardholders")]
    public List<CardholderRequest> Cardholders { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Cardholders == null || Cardholders.Count is < 1 or > 2)
            yield return new ValidationResult("cardholders must contain 1 or 2 items", new[] { nameof(Cardholders) });

        if (TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && TravelcardUsableTo == null)
            yield return new ValidationResult("travelcardUsableTo is required for SixteenToSeventeen", new[] { nameof(TravelcardUsableTo) });
    }
}

public class CardholderRequest : IValidatableObject
{
    [Required]
    [StringLength(15, MinimumLength = 1)]
    [JsonPropertyName("cardholderTitle")]
    public string CardholderTitle { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    [JsonPropertyName("cardholderForename")]
    public string CardholderForename { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    [JsonPropertyName("cardholderSurname")]
    public string CardholderSurname { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("cardholderType")]
    public CardholderTypeEnum CardholderType { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    [JsonPropertyName("cardholderPhotoName")]
    public string CardholderPhotoName { get; set; } = string.Empty;

    [StringLength(42, MinimumLength = 39)]
    [JsonPropertyName("cardholderPhotoRRSKey")]
    public string? CardholderPhotoRRSKey { get; set; }

    [StringLength(2048, MinimumLength = 20)]
    [Url]
    [JsonPropertyName("cardholderPhotoURL")]
    public string? CardholderPhotoURL { get; set; }

    [StringLength(42, MinimumLength = 39)]
    [JsonPropertyName("cardholderPhotoKey")]
    public string? CardholderPhotoKey { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var photoCount = new[] { CardholderPhotoRRSKey, CardholderPhotoURL, CardholderPhotoKey }.Count(p => !string.IsNullOrWhiteSpace(p));
        if (photoCount != 1)
            yield return new ValidationResult("Each cardholder must provide exactly one of: CardholderPhotoRRSKey, CardholderPhotoURL, or CardholderPhotoKey");
    }
}

public class CreateTravelcardResponse
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

public enum TravelcardTypeEnum
{
    Young,
    TwoTogether,
    Family,
    Senior,
    Network,
    TwentySixToThirty,
    SixteenToSeventeen,
    Veterans
}

public enum CardholderTypeEnum
{
    Primary,
    Secondary
}