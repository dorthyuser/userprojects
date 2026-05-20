using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

namespace DemoshauntcLambda.Models;

public sealed class Request
{
    [JsonPropertyName("travelcardType")]
    public TravelcardType TravelcardType { get; set; }

    [JsonPropertyName("travelcardValidFrom")]
    public DateTime TravelcardValidFrom { get; set; }

    [JsonPropertyName("travelcardValidTo")]
    public DateTime TravelcardValidTo { get; set; }

    [JsonPropertyName("travelcardName")]
    public string? TravelcardName { get; set; }

    [JsonPropertyName("travelcardNumber")]
    public string TravelcardNumber { get; set; } = null!;

    [JsonPropertyName("travelcardRequestedDate")]
    public DateTime TravelcardRequestedDate { get; set; }

    [JsonPropertyName("travelcardTransactionReference")]
    public string TravelcardTransactionReference { get; set; } = null!;

    [JsonPropertyName("travelcardUsableTo")]
    public DateTime? TravelcardUsableTo { get; set; }

    [JsonPropertyName("cardholders")]
    public List<CardholderRequest> Cardholders { get; set; } = null!;

    public string? Validate()
    {
        if (!Enum.IsDefined(typeof(TravelcardType), TravelcardType))
            return "Invalid value for field 'travelcardType'. Accepted values: Young, TwoTogether, Family, Senior, Network, TwentySixToThirty, SixteenToSeventeen, Veterans";
        if (TravelcardRequestedDate >= DateTime.UtcNow) return "requested_date must be in the past.";
        if (TravelcardValidFrom >= TravelcardValidTo) return "valid_from date must be later than valid_to date.";
        if (TravelcardValidTo <= DateTime.UtcNow) return "valid_to date must be in the future.";
        if (TravelcardType == TravelcardType.SixteenToSeventeen && TravelcardUsableTo is null) return "usable_to date is required for a SixteenToSeventeen type Travelcard.";
        if (TravelcardUsableTo is not null && TravelcardUsableTo <= DateTime.UtcNow) return "usable_to date must be in the future.";
        if (TravelcardType != TravelcardType.SixteenToSeventeen && TravelcardUsableTo is not null) return "usable_to must not be provided for this Travelcard type.";
        if (Cardholders is null || Cardholders.Count is < 1 or > 2) return "cardholders must contain exactly one primary and optionally one secondary.";
        if (Cardholders.Count(c => c.CardholderType == CardholderType.Primary) != 1) return "Exactly one Primary cardholder is required.";
        if (Cardholders.Count(c => c.CardholderType == CardholderType.Secondary) > 1) return "Only one Secondary cardholder is allowed.";
        if (Cardholders.Any(c => c.CardholderType == CardholderType.Secondary) && TravelcardType is not (TravelcardType.TwoTogether or TravelcardType.Family)) return "Secondary cardholder is not allowed for this Travelcard type.";
        return Cardholders.Select(c => c.Validate()).FirstOrDefault(x => x is not null);
    }
}

public sealed class CardholderRequest
{
    [JsonPropertyName("cardholderTitle")]
    public string CardholderTitle { get; set; } = null!;

    [JsonPropertyName("cardholderForename")]
    public string CardholderForename { get; set; } = null!;

    [JsonPropertyName("cardholderSurname")]
    public string CardholderSurname { get; set; } = null!;

    [JsonPropertyName("cardholderType")]
    public CardholderType CardholderType { get; set; }

    [JsonPropertyName("cardholderPhotoName")]
    public string CardholderPhotoName { get; set; } = null!;

    [JsonPropertyName("cardholderPhotoRRSKey")]
    public string? CardholderPhotoRRSKey { get; set; }

    [JsonPropertyName("cardholderPhotoURL")]
    public string? CardholderPhotoURL { get; set; }

    [JsonPropertyName("cardholderPhotoKey")]
    public string? CardholderPhotoKey { get; set; }

    public string? Validate()
    {
        var oneOf = new[] { CardholderPhotoRRSKey, CardholderPhotoURL, CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
        if (oneOf != 1) return "Each cardholder requires exactly one image detail.";
        return null;
    }
}