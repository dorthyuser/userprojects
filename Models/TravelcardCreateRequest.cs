using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace azuretravelcardapi1218.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TravelcardTypeEnum
{
    Young,
    Barcklays,
    DevonandCornwall,
    TwoTogether,
    Family,
    Senior,
    DisabledPersons,
    Network,
    TwentySixToThirty,
    SixteenToSeventeen,
    Veterans
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardholderTypeEnum
{
    Primary,
    Secondary
}

public sealed class TravelcardCreateRequest : IValidatableObject
{
    [Required]
    public TravelcardTypeEnum TravelcardType { get; set; }

    [Required]
    public DateTimeOffset TravelcardValidFrom { get; set; }

    [Required]
    public DateTimeOffset TravelcardValidTo { get; set; }

    [StringLength(255, MinimumLength = 0)]
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
    public string TravelcardTransactionReference { get; set; } = string.Empty;

    public DateTimeOffset? TravelcardUsableTo { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(2)]
    public List<CardholderRequest> Cardholders { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TravelcardValidFrom >= TravelcardValidTo)
        {
            yield return new ValidationResult("travelcardValidFrom must be earlier than travelcardValidTo", new[] { nameof(TravelcardValidFrom), nameof(TravelcardValidTo) });
        }

        if (TravelcardValidTo <= DateTimeOffset.UtcNow)
        {
            yield return new ValidationResult("travelcardValidTo must be in the future", new[] { nameof(TravelcardValidTo) });
        }

        if (TravelcardRequestedDate >= DateTimeOffset.UtcNow)
        {
            yield return new ValidationResult("travelcardRequestedDate must be in the past", new[] { nameof(TravelcardRequestedDate) });
        }

        if (TravelcardUsableTo.HasValue && TravelcardUsableTo.Value <= DateTimeOffset.UtcNow)
        {
            yield return new ValidationResult("travelcardUsableTo must be in the future", new[] { nameof(TravelcardUsableTo) });
        }

        if (TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && !TravelcardUsableTo.HasValue)
        {
            yield return new ValidationResult("travelcardUsableTo is required for SixteenToSeventeen", new[] { nameof(TravelcardUsableTo) });
        }

        if (TravelcardType != TravelcardTypeEnum.SixteenToSeventeen && TravelcardUsableTo.HasValue)
        {
            yield return new ValidationResult("travelcardUsableTo is only allowed for SixteenToSeventeen", new[] { nameof(TravelcardUsableTo) });
        }

        var primaryCount = Cardholders.Count(x => x.CardholderType == CardholderTypeEnum.Primary);
        var secondaryCount = Cardholders.Count(x => x.CardholderType == CardholderTypeEnum.Secondary);
        if (primaryCount != 1)
        {
            yield return new ValidationResult("Exactly one Primary cardholder is required", new[] { nameof(Cardholders) });
        }

        if (secondaryCount > 1 || Cardholders.Count > 2)
        {
            yield return new ValidationResult("At most one Secondary cardholder is allowed", new[] { nameof(Cardholders) });
        }

        if (secondaryCount == 1 && !(TravelcardType == TravelcardTypeEnum.TwoTogether || TravelcardType == TravelcardTypeEnum.Family))
        {
            yield return new ValidationResult("Secondary cardholder is not allowed for this travelcard type", new[] { nameof(Cardholders) });
        }
    }
}

public sealed class CardholderRequest : IValidatableObject
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
    [Url]
    public string? CardholderPhotoURL { get; set; }

    [StringLength(42, MinimumLength = 39)]
    public string? CardholderPhotoKey { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var count = 0;
        if (!string.IsNullOrWhiteSpace(CardholderPhotoRRSKey)) count++;
        if (!string.IsNullOrWhiteSpace(CardholderPhotoURL)) count++;
        if (!string.IsNullOrWhiteSpace(CardholderPhotoKey)) count++;
        if (count != 1)
        {
            yield return new ValidationResult("Exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey is required", new[] { nameof(CardholderPhotoRRSKey), nameof(CardholderPhotoURL), nameof(CardholderPhotoKey) });
        }
    }
}