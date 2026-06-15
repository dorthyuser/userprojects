using System;
using System.Linq;
using System.Text.RegularExpressions;
using travelcard_functions.Models;

namespace travelcard_functions.Validation;

public static class TravelcardValidator
{
    public static ValidationResult Validate(CreateTravelcardRequest request)
    {
        if (request is null)
        {
            return ValidationResult.Fail("request is null.");
        }

        // Requested date must be in the past
        if (request.TravelcardRequestedDate >= DateTime.UtcNow)
        {
            return ValidationResult.Fail("travelcardRequestedDate must be in the past.");
        }

        // Valid from / to ordering
        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            return ValidationResult.Fail("travelcardValidFrom cannot be later than travelcardValidTo.");
        }

        // ValidTo must be in the future
        if (request.TravelcardValidTo <= DateTime.UtcNow)
        {
            return ValidationResult.Fail("travelcardValidTo must be in the future.");
        }

        // ValidFrom not more than one calendar month ahead
        if (request.TravelcardValidFrom > DateTime.UtcNow.AddMonths(1))
        {
            return ValidationResult.Fail("travelcardValidFrom must not be later than one calendar month from now.");
        }

        // UsableTo required for certain types
        if (request.TravelcardType == TravelcardType.SixteenToSeventeen && request.TravelcardUsableTo is null)
        {
            return ValidationResult.Fail("travelcardUsableTo is required for SixteenToSeventeen travelcards.");
        }

        if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTime.UtcNow)
        {
            return ValidationResult.Fail("travelcardUsableTo must be in the future.");
        }

        // Cardholders count
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

        // travelcardName: optional but if present must be valid
        if (!string.IsNullOrWhiteSpace(request.TravelcardName))
        {
            var name = request.TravelcardName!.Trim();
            if (name.Length == 0 || name.Length > 255 || !Regex.IsMatch(name, "^[A-Za-z0-9 ]*$"))
            {
                return ValidationResult.Fail("travelcardName is invalid.");
            }
        }

        // travelcardNumber: trim, allow alphanumeric, length between 10 and 22 (inclusive)
        var travelcardNumber = (request.TravelcardNumber ?? string.Empty).Trim();
        if (travelcardNumber.Length < 10 || travelcardNumber.Length > 22 || !Regex.IsMatch(travelcardNumber, "^[A-Za-z0-9]+$"))
        {
            return ValidationResult.Fail("travelcardNumber is invalid.");
        }

        // transaction reference must match pattern (trimmed)
        var txRef = (request.TravelcardTransactionReference ?? string.Empty).Trim();
        if (txRef.Length != 15 || !Regex.IsMatch(txRef, "^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$"))
        {
            return ValidationResult.Fail("travelcardTransactionReference is invalid.");
        }

        // Cardholder-level validation (trim fields before checking lengths)
        foreach (var cardholder in request.Cardholders)
        {
            var title = (cardholder.CardholderTitle ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title) || title.Length > 15)
            {
                return ValidationResult.Fail("cardholderTitle is invalid.");
            }

            var forename = (cardholder.CardholderForename ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(forename) || forename.Length > 100)
            {
                return ValidationResult.Fail("cardholderForename is invalid.");
            }

            var surname = (cardholder.CardholderSurname ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(surname) || surname.Length > 100)
            {
                return ValidationResult.Fail("cardholderSurname is invalid.");
            }

            var photoName = (cardholder.CardholderPhotoName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(photoName) || photoName.Length > 100)
            {
                return ValidationResult.Fail("cardholderPhotoName is invalid.");
            }

            var rrs = (cardholder.CardholderPhotoRRSKey ?? string.Empty).Trim();
            var url = (cardholder.CardholderPhotoURL ?? string.Empty).Trim();
            var key = (cardholder.CardholderPhotoKey ?? string.Empty).Trim();

            var oneOfCount = 0;
            if (!string.IsNullOrWhiteSpace(rrs)) oneOfCount++;
            if (!string.IsNullOrWhiteSpace(url)) oneOfCount++;
            if (!string.IsNullOrWhiteSpace(key)) oneOfCount++;
            if (oneOfCount != 1)
            {
                return ValidationResult.Fail("Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey.");
            }
        }

        return ValidationResult.Ok();
    }
}
