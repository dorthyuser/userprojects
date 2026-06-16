using System;
using System.Linq;
using System.Text.RegularExpressions;
using travelcard_functions.Models;

namespace travelcard_functions.Validation;

public static class TravelcardValidator
{
    public static ValidationResult Validate(CreateTravelcardRequest request)
    {
        // Defensive: ensure request is not null
        if (request == null) return ValidationResult.Fail("request is null");

        // Normalize / trim inputs to avoid failing on leading/trailing whitespace
        request.TravelcardNumber = (request.TravelcardNumber ?? string.Empty).Trim();
        request.TravelcardName = request.TravelcardName?.Trim();
        request.TravelcardTransactionReference = (request.TravelcardTransactionReference ?? string.Empty).Trim();

        if (request.Cardholders != null)
        {
            foreach (var ch in request.Cardholders)
            {
                if (ch == null) return ValidationResult.Fail("cardholder entry is null.");
                ch.CardholderTitle = (ch.CardholderTitle ?? string.Empty).Trim();
                ch.CardholderForename = (ch.CardholderForename ?? string.Empty).Trim();
                ch.CardholderSurname = (ch.CardholderSurname ?? string.Empty).Trim();
                ch.CardholderPhotoName = (ch.CardholderPhotoName ?? string.Empty).Trim();
                ch.CardholderPhotoRRSKey = ch.CardholderPhotoRRSKey?.Trim();
                ch.CardholderPhotoURL = ch.CardholderPhotoURL?.Trim();
                ch.CardholderPhotoKey = ch.CardholderPhotoKey?.Trim();
            }
        }

        // Requested date should be in the past (allow small clock skew)
        if (request.TravelcardRequestedDate >= DateTime.UtcNow.AddSeconds(5))
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

        if (request.TravelcardName is not null && (request.TravelcardName.Length > 255 || !Regex.IsMatch(request.TravelcardName, @"^[A-Za-z0-9 '.,-]*$")))
        {
            return ValidationResult.Fail("travelcardName is invalid.");
        }

        // Allow common separators in travelcard numbers (spaces, hyphens) by removing them before validation
        var travelcardNumberClean = Regex.Replace(request.TravelcardNumber ?? string.Empty, "[^A-Za-z0-9]", string.Empty).ToUpperInvariant();

        // Enforce DB-specific pattern: 3 uppercase letters followed by 8 digits (e.g. ABC12345678)
        if (!Regex.IsMatch(travelcardNumberClean, "^[A-Z]{3}[0-9]{8}$"))
        {
            return ValidationResult.Fail("travelcardNumber is invalid.");
        }

        // Transaction reference: expected 15 chars (2 digits + 4 alnum + 4 digits + 5 digits)
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

            // Allow common name characters (letters, spaces, hyphens, apostrophes) and also periods/commas
            if (string.IsNullOrWhiteSpace(cardholder.CardholderSurname) || cardholder.CardholderSurname.Length > 100 ||
                !Regex.IsMatch(cardholder.CardholderSurname, "^[A-Za-zÀ-ÖØ-öø-ÿ' .,-]+$"))
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
