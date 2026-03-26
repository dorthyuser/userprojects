using System;
using System.Linq;
using TravelcardFunction.Models;

namespace TravelcardFunction.Functions
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    public class RequestValidator
    {
        public ValidationResult Validate(TravelcardRequest request)
        {
            if (request == null) return new ValidationResult { IsValid = false, Error = "Request body is required" };

            // travelcardType required
            if (!Enum.IsDefined(typeof(TravelcardType), request.TravelcardType))
                return new ValidationResult { IsValid = false, Error = "Invalid travelcardType" };

            var now = DateTime.UtcNow;

            // requested_date in the past
            if (request.TravelcardRequestedDate > now)
                return new ValidationResult { IsValid = false, Error = "travelcardRequestedDate must be in the past" };

            // valid_from later than valid_to invalid -> if valid_from > valid_to -> error
            if (request.TravelcardValidFrom > request.TravelcardValidTo)
                return new ValidationResult { IsValid = false, Error = "travelcardValidFrom cannot be later than travelcardValidTo" };

            // valid_to in the future
            if (request.TravelcardValidTo <= now)
                return new ValidationResult { IsValid = false, Error = "travelcardValidTo must be in the future" };

            // usable_to required for SixteenToSeventeen
            if (request.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!request.TravelcardUsableTo.HasValue)
                    return new ValidationResult { IsValid = false, Error = "travelcardUsableTo is required for SixteenToSeventeen" };
                if (request.TravelcardUsableTo.Value <= now)
                    return new ValidationResult { IsValid = false, Error = "travelcardUsableTo must be in the future" };
            }
            else
            {
                if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= now)
                    return new ValidationResult { IsValid = false, Error = "travelcardUsableTo must be in the future if provided" };
            }

            // travelcardNumber length
            if (string.IsNullOrWhiteSpace(request.TravelcardNumber) || request.TravelcardNumber.Length < 11 || request.TravelcardNumber.Length > 22)
                return new ValidationResult { IsValid = false, Error = "travelcardNumber must be between 11 and 22 characters" };

            // transaction reference length exactly 15
            if (string.IsNullOrWhiteSpace(request.TravelcardTransactionReference) || request.TravelcardTransactionReference.Length != 15)
                return new ValidationResult { IsValid = false, Error = "travelcardTransactionReference must be exactly 15 characters" };

            // cardholders 1 or 2 only
            if (request.Cardholders == null || request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
                return new ValidationResult { IsValid = false, Error = "cardholders must contain exactly 1 or 2 items" };

            // Exactly one Primary
            var primaryCount = request.Cardholders.Count(c => c.CardholderType == CardholderType.Primary);
            if (primaryCount != 1) return new ValidationResult { IsValid = false, Error = "There must be exactly one Primary cardholder" };

            // Secondary allowed check: business rule - only allowed for TwoTogether and Family
            var hasSecondary = request.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary);
            if (hasSecondary)
            {
                if (!(request.TravelcardType == TravelcardType.TwoTogether || request.TravelcardType == TravelcardType.Family))
                    return new ValidationResult { IsValid = false, Error = "Secondary cardholder is only allowed for TwoTogether and Family travelcard types" };
            }

            // Validate each cardholder fields
            foreach (var ch in request.Cardholders)
            {
                if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                    return new ValidationResult { IsValid = false, Error = "Invalid cardholderTitle" };
                if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                    return new ValidationResult { IsValid = false, Error = "Invalid cardholderForename" };
                if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                    return new ValidationResult { IsValid = false, Error = "Invalid cardholderSurname" };
                if (!Enum.IsDefined(typeof(CardholderType), ch.CardholderType))
                    return new ValidationResult { IsValid = false, Error = "Invalid cardholderType" };
                if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                    return new ValidationResult { IsValid = false, Error = "Invalid cardholderPhotoName" };

                // OneOf photo fields
                var photoProvided = !string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) || !string.IsNullOrWhiteSpace(ch.CardholderPhotoURL) || !string.IsNullOrWhiteSpace(ch.CardholderPhotoKey);
                if (!photoProvided)
                    return new ValidationResult { IsValid = false, Error = "Each cardholder must provide one photo field (photo rrsk, url or key)" };

                if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) && (ch.CardholderPhotoRRSKey.Length < 39 || ch.CardholderPhotoRRSKey.Length > 42))
                    return new ValidationResult { IsValid = false, Error = "cardholderPhotoRRSKey length invalid" };
                if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) && (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42))
                    return new ValidationResult { IsValid = false, Error = "cardholderPhotoKey length invalid" };
                if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoURL) && (ch.CardholderPhotoURL.Length < 20 || ch.CardholderPhotoURL.Length > 2048))
                    return new ValidationResult { IsValid = false, Error = "cardholderPhotoURL length invalid" };
            }

            return new ValidationResult { IsValid = true };
        }
    }
}
