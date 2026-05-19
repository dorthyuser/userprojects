using System;
using System.Linq;
using TravelCardFunctionApp.Models;

namespace TravelCardFunctionApp.Helpers;

public static class TravelcardValidator
{
    public static ValidationResult Validate(CreateTravelcardRequest request)
    {
        if (request.TravelcardRequestedDate >= DateTime.UtcNow)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "travelcardRequestedDate must be in the past." };
        }

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "travelcardValidFrom must not be later than travelcardValidTo." };
        }

        if (request.TravelcardValidTo <= DateTime.UtcNow)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "travelcardValidTo must be in the future." };
        }

        if (request.TravelcardValidFrom > DateTime.UtcNow.AddMonths(1))
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "travelcardValidFrom must not be later than one calendar month from today." };
        }

        if (request.TravelcardType == travelcard_type_enum.SixteenToSeventeen)
        {
            if (!request.TravelcardUsableTo.HasValue)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "travelcardUsableTo is required for SixteenToSeventeen travelcards." };
            }

            if (request.TravelcardUsableTo.Value <= DateTime.UtcNow)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "travelcardUsableTo must be in the future." };
            }
        }
        else if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTime.UtcNow)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "travelcardUsableTo must be in the future." };
        }

        if (request.TravelcardType is travelcard_type_enum.SixteenToSeventeen or travelcard_type_enum.Veterans)
        {
            if (request.Cardholders.Any(c => c.CardholderType == cardholder_type_enum.Secondary))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Secondary cardholder is not allowed for SixteenToSeventeen and Veterans travelcards." };
            }
        }

        if (request.Cardholders.Count is < 1 or > 2)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "cardholders must contain exactly one or two items." };
        }

        if (!request.Cardholders.Any(c => c.CardholderType == cardholder_type_enum.Primary))
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "Exactly one Primary cardholder is required." };
        }

        if (request.Cardholders.Count(c => c.CardholderType == cardholder_type_enum.Secondary) > 1)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "Only one Secondary cardholder is allowed." };
        }

        foreach (var cardholder in request.Cardholders)
        {
            if (string.IsNullOrWhiteSpace(cardholder.CardholderTitle) || string.IsNullOrWhiteSpace(cardholder.CardholderForename) || string.IsNullOrWhiteSpace(cardholder.CardholderSurname) || string.IsNullOrWhiteSpace(cardholder.CardholderPhotoName))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Cardholder required fields must be provided." };
            }

            var photoSourceCount = new[] { cardholder.CardholderPhotoRRSKey, cardholder.CardholderPhotoURL, cardholder.CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
            if (photoSourceCount != 1)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Each cardholder must provide exactly one photo source." };
            }
        }

        return new ValidationResult { IsValid = true };
    }
}
