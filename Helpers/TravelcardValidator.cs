using System;
using System.Linq;
using Models;

namespace Helpers
{
    public static class TravelcardValidator
    {
        public static ValidationResult Validate(CreateTravelcardRequest request)
        {
            if (request.TravelcardRequestedDate >= DateTime.UtcNow)
            {
                return new ValidationResult(false, "travelcardRequestedDate must be in the past.");
            }

            if (request.TravelcardValidFrom > request.TravelcardValidTo)
            {
                return new ValidationResult(false, "travelcardValidFrom must be earlier than or equal to travelcardValidTo.");
            }

            if (request.TravelcardValidTo <= DateTime.UtcNow)
            {
                return new ValidationResult(false, "travelcardValidTo must be in the future.");
            }

            if (request.TravelcardType == travelcardType_enum.SixteenToSeventeen)
            {
                if (!request.TravelcardUsableTo.HasValue)
                {
                    return new ValidationResult(false, "travelcardUsableTo is required for SixteenToSeventeen travelcard type.");
                }
            }

            if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTime.UtcNow)
            {
                return new ValidationResult(false, "travelcardUsableTo must be in the future.");
            }

            if (request.Cardholders == null || request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
            {
                return new ValidationResult(false, "cardholders must contain exactly one or two items.");
            }

            if (!request.Cardholders.Any(x => x.CardholderType == cardholderType_enum.Primary))
            {
                return new ValidationResult(false, "Exactly one Primary cardholder is required.");
            }

            if (request.Cardholders.Count(x => x.CardholderType == cardholderType_enum.Secondary) > 1)
            {
                return new ValidationResult(false, "Only one Secondary cardholder is allowed.");
            }

            if (request.Cardholders.Any(x => x.CardholderType == cardholderType_enum.Secondary) && request.TravelcardType == travelcardType_enum.Veterans)
            {
                return new ValidationResult(false, "Secondary cardholder is not allowed for this travelcard type.");
            }

            foreach (CardholderRequest cardholder in request.Cardholders)
            {
                if (string.IsNullOrWhiteSpace(cardholder.CardholderTitle) || cardholder.CardholderTitle.Length > 15)
                {
                    return new ValidationResult(false, "cardholderTitle is required and must not exceed 15 characters.");
                }

                if (string.IsNullOrWhiteSpace(cardholder.CardholderForename) || cardholder.CardholderForename.Length > 100)
                {
                    return new ValidationResult(false, "cardholderForename is required and must not exceed 100 characters.");
                }

                if (string.IsNullOrWhiteSpace(cardholder.CardholderSurname) || cardholder.CardholderSurname.Length > 100)
                {
                    return new ValidationResult(false, "cardholderSurname is required and must not exceed 100 characters.");
                }

                if (string.IsNullOrWhiteSpace(cardholder.CardholderPhotoName) || cardholder.CardholderPhotoName.Length > 100)
                {
                    return new ValidationResult(false, "cardholderPhotoName is required and must not exceed 100 characters.");
                }

                bool hasRrs = !string.IsNullOrWhiteSpace(cardholder.CardholderPhotoRRSKey);
                bool hasUrl = !string.IsNullOrWhiteSpace(cardholder.CardholderPhotoURL);
                bool hasKey = !string.IsNullOrWhiteSpace(cardholder.CardholderPhotoKey);
                if (new[] { hasRrs, hasUrl, hasKey }.Count(x => x) != 1)
                {
                    return new ValidationResult(false, "Each cardholder must provide exactly one image detail field.");
                }
            }

            return new ValidationResult(true, string.Empty);
        }
    }
}