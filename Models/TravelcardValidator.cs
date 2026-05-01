using System;
using System.Linq;

namespace azuretravelcardfunction309.Models;

public static class TravelcardValidator
{
    public static string? Validate(CreateTravelcardRequest request)
    {
        if (request.travelcardRequestedDate >= DateTime.UtcNow.AddMinutes(1)) return "travelcardRequestedDate must be in the past.";
        if (request.travelcardValidFrom > request.travelcardValidTo) return "travelcardValidFrom must be earlier than travelcardValidTo.";
        if (request.travelcardValidTo <= DateTime.UtcNow) return "travelcardValidTo must be in the future.";
        if (request.travelcardType == travelcardType_enum.SixteenToSeventeen && request.travelcardUsableTo is null) return "travelcardUsableTo is required for SixteenToSeventeen travelcards.";
        if (request.travelcardUsableTo.HasValue && request.travelcardUsableTo.Value <= DateTime.UtcNow) return "travelcardUsableTo must be in the future when provided.";
        if (request.cardholders is null || request.cardholders.Count < 1 || request.cardholders.Count > 2) return "cardholders must contain exactly 1 or 2 items.";
        if (!request.cardholders.Any(c => c.cardholderType == cardholderType_enum.Primary)) return "One Primary cardholder is required.";
        if (request.cardholders.Count(c => c.cardholderType == cardholderType_enum.Primary) != 1) return "Exactly one Primary cardholder is required.";
        if (request.cardholders.Count(c => c.cardholderType == cardholderType_enum.Secondary) > 1) return "At most one Secondary cardholder is allowed.";
        if (request.cardholders.Count(c => c.cardholderType == cardholderType_enum.Secondary) == 1 && request.travelcardType != travelcardType_enum.TwoTogether) return "Secondary cardholder is only allowed for TwoTogether travelcards.";

        foreach (var cardholder in request.cardholders)
        {
            var error = ValidateCardholder(cardholder);
            if (!string.IsNullOrWhiteSpace(error)) return error;

            var sources = new[] { cardholder.cardholderPhotoRRSKey, cardholder.cardholderPhotoURL, cardholder.cardholderPhotoKey };
            if (sources.Count(s => !string.IsNullOrWhiteSpace(s)) != 1) return "Each cardholder must provide exactly one photo source: cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey.";
        }

        return null;
    }

    private static string? ValidateCardholder(CardholderRequest cardholder)
    {
        if (string.IsNullOrWhiteSpace(cardholder.cardholderTitle) || cardholder.cardholderTitle.Length > 15) return "cardholderTitle must be 1 to 15 characters.";
        if (string.IsNullOrWhiteSpace(cardholder.cardholderForename) || cardholder.cardholderForename.Length > 100) return "cardholderForename must be 1 to 100 characters.";
        if (string.IsNullOrWhiteSpace(cardholder.cardholderSurname) || cardholder.cardholderSurname.Length > 100) return "cardholderSurname must be 1 to 100 characters.";
        if (string.IsNullOrWhiteSpace(cardholder.cardholderPhotoName) || cardholder.cardholderPhotoName.Length > 100) return "cardholderPhotoName must be 1 to 100 characters.";
        return null;
    }
}
