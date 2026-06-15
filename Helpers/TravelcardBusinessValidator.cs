using System;
using travelcard_function_app.Models;

namespace travelcard_function_app.Helpers;

public static class TravelcardBusinessValidator
{
    public static ValidationResult Validate(CreateTravelcardRequest request)
    {
        if (request.TravelcardRequestedDate >= DateTime.UtcNow)
        {
            return ValidationResult.Fail("travelcardRequestedDate must be in the past", "INVALID_REQUESTED_DATE");
        }

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            return ValidationResult.Fail("travelcardValidFrom must not be later than travelcardValidTo", "INVALID_VALIDITY_RANGE");
        }

        if (request.TravelcardValidTo <= DateTime.UtcNow)
        {
            return ValidationResult.Fail("travelcardValidTo must be in the future", "INVALID_VALID_TO");
        }

        if (request.TravelcardValidFrom > DateTime.UtcNow.AddMonths(1))
        {
            return ValidationResult.Fail("travelcardValidFrom must be no later than one calendar month from now", "INVALID_VALID_FROM");
        }

        if (request.TravelcardType == TravelcardType.SixteenToSeventeen)
        {
            if (request.TravelcardUsableTo == null)
            {
                return ValidationResult.Fail("travelcardUsableTo is required for SixteenToSeventeen travelcards", "MISSING_USABLE_TO");
            }

            if (request.TravelcardUsableTo <= DateTime.UtcNow)
            {
                return ValidationResult.Fail("travelcardUsableTo must be in the future", "INVALID_USABLE_TO");
            }
        }
        else if (request.TravelcardUsableTo != null)
        {
            return ValidationResult.Fail("travelcardUsableTo is only allowed for SixteenToSeventeen travelcards", "INVALID_USABLE_TO_USAGE");
        }

        if (request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
        {
            return ValidationResult.Fail("cardholders must contain exactly one or two items", "INVALID_CARDHOLDER_COUNT");
        }

        var primaryCount = 0;
        var secondaryCount = 0;
        foreach (var cardholder in request.Cardholders)
        {
            if (cardholder.CardholderType == CardholderType.Primary) primaryCount++;
            if (cardholder.CardholderType == CardholderType.Secondary) secondaryCount++;
        }

        if (primaryCount != 1)
        {
            return ValidationResult.Fail("Exactly one Primary cardholder is required", "INVALID_PRIMARY_CARDHOLDER");
        }

        if (secondaryCount > 1)
        {
            return ValidationResult.Fail("Only one Secondary cardholder is allowed", "INVALID_SECONDARY_CARDHOLDER");
        }

        if ((request.TravelcardType == TravelcardType.SixteenToSeventeen || request.TravelcardType == TravelcardType.Veterans) && secondaryCount > 0)
        {
            return ValidationResult.Fail("Secondary cardholder is not allowed for SixteenToSeventeen and Veterans travelcards", "SECONDARY_NOT_ALLOWED");
        }

        return ValidationResult.Ok();
    }
}
