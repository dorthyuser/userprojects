using System.Text.Json.Serialization;

namespace azurecsharpfunction534.Models;

public class CreateTravelcardRequest
{
    [JsonPropertyName("travelcardType")]
    public string TravelcardType { get; set; } = string.Empty;

    [JsonPropertyName("travelcardValidFrom")]
    public DateTimeOffset TravelcardValidFrom { get; set; }

    [JsonPropertyName("travelcardValidTo")]
    public DateTimeOffset TravelcardValidTo { get; set; }

    [JsonPropertyName("travelcardName")]
    public string? TravelcardName { get; set; }

    [JsonPropertyName("travelcardNumber")]
    public string TravelcardNumber { get; set; } = string.Empty;

    [JsonPropertyName("travelcardRequestedDate")]
    public DateTimeOffset TravelcardRequestedDate { get; set; }

    [JsonPropertyName("travelcardTransactionReference")]
    public string TravelcardTransactionReference { get; set; } = string.Empty;

    [JsonPropertyName("travelcardUsableTo")]
    public DateTimeOffset? TravelcardUsableTo { get; set; }

    [JsonPropertyName("cardholders")]
    public List<CardholderRequest> Cardholders { get; set; } = new();

    public string? Validate()
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "Young","Barcklays","DevonandCornwall","TwoTogether","Family","Senior","DisabledPersons","Network","TwentySixToThirty","SixteenToSeventeen","Veterans"
        };

        if (!allowed.Contains(TravelcardType)) return "travelcardType is invalid.";
        if (TravelcardValidFrom <= TravelcardValidTo) return "travelcardValidFrom must be later than travelcardValidTo.";
        if (TravelcardValidTo <= DateTimeOffset.UtcNow) return "travelcardValidTo must be in the future.";
        if (TravelcardRequestedDate >= DateTimeOffset.UtcNow) return "travelcardRequestedDate must be in the past.";
        if (TravelcardType == "SixteenToSeventeen" && TravelcardUsableTo is null) return "travelcardUsableTo is required for SixteenToSeventeen travelcards.";
        if (TravelcardUsableTo is not null && TravelcardUsableTo <= DateTimeOffset.UtcNow) return "travelcardUsableTo must be in the future.";
        if (TravelcardType != "SixteenToSeventeen" && TravelcardUsableTo is not null) return "travelcardUsableTo is only allowed for SixteenToSeventeen travelcards.";
        if (Cardholders is null || Cardholders.Count is < 1 or > 2) return "cardholders must contain exactly one or two items.";

        var primaryCount = Cardholders.Count(x => x.CardholderType == "Primary");
        var secondaryCount = Cardholders.Count(x => x.CardholderType == "Secondary");
        if (primaryCount != 1) return "Exactly one Primary cardholder is required.";
        if (secondaryCount > 1) return "Only one Secondary cardholder is allowed.";
        if (secondaryCount == 1 && TravelcardType is "Young" or "Senior" or "Veterans") return "Secondary cardholder is not allowed for this travelcard type.";

        foreach (var ch in Cardholders)
        {
            var err = ch.Validate();
            if (!string.IsNullOrEmpty(err)) return err;
        }

        return null;
    }
}

public class CardholderRequest
{
    [JsonPropertyName("cardholderTitle")]
    public string CardholderTitle { get; set; } = string.Empty;

    [JsonPropertyName("cardholderForename")]
    public string CardholderForename { get; set; } = string.Empty;

    [JsonPropertyName("cardholderSurname")]
    public string CardholderSurname { get; set; } = string.Empty;

    [JsonPropertyName("cardholderType")]
    public string CardholderType { get; set; } = string.Empty;

    [JsonPropertyName("cardholderPhotoName")]
    public string CardholderPhotoName { get; set; } = string.Empty;

    [JsonPropertyName("cardholderPhotoRRSKey")]
    public string? CardholderPhotoRRSKey { get; set; }

    [JsonPropertyName("cardholderPhotoURL")]
    public string? CardholderPhotoURL { get; set; }

    [JsonPropertyName("cardholderPhotoKey")]
    public string? CardholderPhotoKey { get; set; }

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(CardholderTitle) || CardholderTitle.Length > 15) return "cardholderTitle is invalid.";
        if (string.IsNullOrWhiteSpace(CardholderForename) || CardholderForename.Length > 100) return "cardholderForename is invalid.";
        if (string.IsNullOrWhiteSpace(CardholderSurname) || CardholderSurname.Length > 100) return "cardholderSurname is invalid.";
        if (CardholderType is not ("Primary" or "Secondary")) return "cardholderType is invalid.";
        if (string.IsNullOrWhiteSpace(CardholderPhotoName) || CardholderPhotoName.Length > 100) return "cardholderPhotoName is invalid.";

        var provided = new[] { CardholderPhotoRRSKey, CardholderPhotoURL, CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
        if (provided != 1) return "Exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey must be provided.";
        return null;
    }
}

public class CreateTravelcardResponse
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;
}