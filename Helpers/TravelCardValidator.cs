using System;
using System.Linq;
using TravelCardFunctionApp.Models;

namespace TravelCardFunctionApp.Helpers;

public static class TravelCardValidator
{
    public static ValidationResult Validate(CreateTravelCardRequest request)
    {
        if (request.TravelcardRequestedDate >= DateTime.UtcNow)
        {
            return ValidationResult.Fail("travelcardRequestedDate must be in the past.");
        }

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            return ValidationResult.Fail("travelcardValidFrom must be earlier than travelcardValidTo.");
        }

        if (request.TravelcardValidTo <= DateTime.UtcNow)
        {
            return ValidationResult.Fail("travelcardValidTo must be in the future.");
        }

        if (request.TravelcardType == TravelcardType.SixteenToSeventeen && request.TravelcardUsableTo is null)
        {
            return ValidationResult.Fail("travelcardUsableTo is required for SixteenToSeventeen travelcards.");
        }

        if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTime.UtcNow)
        {
            return ValidationResult.Fail("travelcardUsableTo must be in the future when provided.");
        }

        if (request.Cardholders.Count is < 1 or > 2)
        {
            return ValidationResult.Fail("cardholders must contain exactly one primary and optionally one secondary cardholder.");
        }

        if (!request.Cardholders.Any(x => x.CardholderType == CardholderType.Primary))
        {
            return ValidationResult.Fail("A primary cardholder is required.");
        }

        if (request.Cardholders.Count(x => x.CardholderType == CardholderType.Secondary) > 1)
        {
            return ValidationResult.Fail("Only one secondary cardholder is allowed.");
        }

        if (request.Cardholders.Count(x => x.CardholderType == CardholderType.Secondary) == 1 && request.TravelcardType != TravelcardType.Family)
        {
            return ValidationResult.Fail("Secondary cardholder is only allowed for Family travelcards.");
        }

        foreach (var cardholder in request.Cardholders)
        {
            var photoCount = new[] { cardholder.CardholderPhotoRRSKey, cardholder.CardholderPhotoURL, cardholder.CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
            if (photoCount != 1)
            {
                return ValidationResult.Fail("Each cardholder must provide exactly one photo identifier.");
            }
        }

        return ValidationResult.Success();
    }
}
