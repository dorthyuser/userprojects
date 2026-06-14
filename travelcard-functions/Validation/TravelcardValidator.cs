using System;
using System.Linq;
using System.Text.RegularExpressions;
using travelcard_functions.Models;

namespace travelcard_functions.Validation;

public static class TravelcardValidator
{
    public static ValidationResult Validate(CreateTravelcardRequest request)
    {
        if (request.TravelcardRequestedDate >= DateTime.UtcNow)
        {
            return ValidationResult.Fail("travelcardRequestedDate must be in the past.");
        }

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            return ValidationResult.Fail("travelcardValidFrom cannot be later than travelcardValidTo.");
        }

        if (request.TravelcardValidTo <= DateTime.UtcNow)
        {
            return ValidationResult.Fail("travelcardValidTo must be in the future.");
        }

        if (request.TravelcardValidFrom > DateTime.UtcNow.AddMonths(1))
        {
            return ValidationResult.Fail("travelcardValidFrom must not be later than one calendar month from now.");
        }

        if (request.TravelcardType == TravelcardType.SixteenToSeventeen && request.TravelcardUsableTo is null)
        {
            return ValidationResult.Fail("travelcardUsableTo is required for SixteenToSeventeen travelcards.");
        }

        if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTime.UtcNow)
        {
            return ValidationResult.Fail("travelcardUsableTo must be in the future.");
        }

        if (request.Cardholders is null || request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
        {
            return ValidationResult.Fail("cardholders must contain exactly one or two items.");
        }

        var primaryCount = request.Cardholders.Count(c => c.CardholderType == CardholderType.Primary);
        var secondaryCount = request.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary);
        if (primaryCount != 1 || secondaryCount > 1)
        {
            return ValidationResult.Fail("Exactly one Primary cardholder and at most one Secondary cardholder are required.");
        }

        if ((request.TravelcardType == TravelcardType.SixteenToSeventeen || request.TravelcardType == TravelcardType.Veterans) && secondaryCount == 1)
        {
            return ValidationResult.Fail("Secondary cardholder is not allowed for SixteenToSeventeen and Veterans travelcards.");
        }

        if (request.TravelcardName is not null && (request.TravelcardName.Length > 255 || !Regex.IsMatch(request.TravelcardName, "^[A-Za-z0-9 ]*$")))
        {
            return ValidationResult.Fail("travelcardName is invalid.");
        }

        if (request.TravelcardNumber.Length < 11 || request.TravelcardNumber.Length > 22 || !Regex.IsMatch(request.TravelcardNumber, "^[A-Za-z0-9]+$"))
        {
            return ValidationResult.Fail("travelcardNumber is invalid.");
        }

        if (request.TravelcardTransactionReference.Length != 15 || !Regex.IsMatch(request.TravelcardTransactionReference, "^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$"))
        {
            return ValidationResult.Fail("travelcardTransactionReference is invalid.");
        }

        foreach (var cardholder in request.Cardholders)
        {
            if (string.IsNullOrWhiteSpace(cardholder.CardholderTitle) || cardholder.CardholderTitle.Length > 15)
            {
                return ValidationResult.Fail("cardholderTitle is invalid.");
            }

            if (string.IsNullOrWhiteSpace(cardholder.CardholderForename) || cardholder.CardholderForename.Length > 100)
            {
                return ValidationResult.Fail("cardholderForename is invalid.");
            }

            if (string.IsNullOrWhiteSpace(cardholder.CardholderSurname) || cardholder.CardholderSurname.Length > 100)
            {
                return ValidationResult.Fail("cardholderSurname is invalid.");
            }

            if (string.IsNullOrWhiteSpace(cardholder.CardholderPhotoName) || cardholder.CardholderPhotoName.Length > 100)
            {
                return ValidationResult.Fail("cardholderPhotoName is invalid.");
            }

            var oneOfCount = 0;
            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoRRSKey)) oneOfCount++;
            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoURL)) oneOfCount++;
            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoKey)) oneOfCount++;
            if (oneOfCount != 1)
            {
                return ValidationResult.Fail("Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey.");
            }
        }

        return ValidationResult.Ok();
    }
}
