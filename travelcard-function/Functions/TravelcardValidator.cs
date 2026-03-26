using System;
using travelcard_function.Models;

namespace travelcard_function.Functions
{
    public class TravelcardValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class TravelcardValidator
    {
        public TravelcardValidationResult Validate(CreateTravelcardRequest req)
        {
            var now = DateTime.UtcNow;
            if (req == null)
            {
                return new TravelcardValidationResult { IsValid = false, ErrorMessage = "Request is null" };
            }

            if (req.travelcardNumber == null || req.travelcardNumber.Length < 11 || req.travelcardNumber.Length > 22)
                return new TravelcardValidationResult { IsValid = false, ErrorMessage = "travelcardNumber must be between 11 and 22 characters" };

            if (string.IsNullOrWhiteSpace(req.travelcardTransactionReference) || req.travelcardTransactionReference.Length != 15)
                return new TravelcardValidationResult { IsValid = false, ErrorMessage = "travelcardTransactionReference must be exactly 15 characters" };

            if (req.travelcardRequestedDate >= now)
                return new TravelcardValidationResult { IsValid = false, ErrorMessage = "Requested date must be in the past" };

            if (req.travelcardValidFrom > req.travelcardValidTo)
                return new TravelcardValidationResult { IsValid = false, ErrorMessage = "travelcardValidFrom cannot be later than travelcardValidTo" };

            if (req.travelcardValidTo <= now)
                return new TravelcardValidationResult { IsValid = false, ErrorMessage = "travelcardValidTo must be in the future" };

            if (req.travelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!req.travelcardUsableTo.HasValue)
                    return new TravelcardValidationResult { IsValid = false, ErrorMessage = "travelcardUsableTo is required for SixteenToSeventeen" };
                if (req.travelcardUsableTo.Value <= now)
                    return new TravelcardValidationResult { IsValid = false, ErrorMessage = "travelcardUsableTo must be in the future" };
            }

            if (req.cardholders == null || req.cardholders.Length < 1 || req.cardholders.Length > 2)
                return new TravelcardValidationResult { IsValid = false, ErrorMessage = "cardholders must contain 1 or 2 items" };

            if (req.cardholders.Length == 2)
            {
                if (!(req.travelcardType == TravelcardType.Family || req.travelcardType == TravelcardType.TwoTogether))
                    return new TravelcardValidationResult { IsValid = false, ErrorMessage = "Secondary cardholder allowed only for Family or TwoTogether travelcard types" };
            }

            foreach (var ch in req.cardholders)
            {
                if (string.IsNullOrWhiteSpace(ch.cardholderTitle) || ch.cardholderTitle.Length > 15)
                    return new TravelcardValidationResult { IsValid = false, ErrorMessage = "Invalid cardholderTitle" };
                if (string.IsNullOrWhiteSpace(ch.cardholderForename) || ch.cardholderForename.Length > 100)
                    return new TravelcardValidationResult { IsValid = false, ErrorMessage = "Invalid cardholderForename" };
                if (string.IsNullOrWhiteSpace(ch.cardholderSurname) || ch.cardholderSurname.Length > 100)
                    return new TravelcardValidationResult { IsValid = false, ErrorMessage = "Invalid cardholderSurname" };
                if (string.IsNullOrWhiteSpace(ch.cardholderPhotoName) || ch.cardholderPhotoName.Length > 100)
                    return new TravelcardValidationResult { IsValid = false, ErrorMessage = "Invalid cardholderPhotoName" };

                var provided = 0;
                if (!string.IsNullOrWhiteSpace(ch.cardholderPhotoRRSKey)) provided++;
                if (!string.IsNullOrWhiteSpace(ch.cardholderPhotoURL)) provided++;
                if (!string.IsNullOrWhiteSpace(ch.cardholderPhotoKey)) provided++;
                if (provided != 1)
                    return new TravelcardValidationResult { IsValid = false, ErrorMessage = "Each cardholder must provide exactly one photo identifier (cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey)" };
            }

            return new TravelcardValidationResult { IsValid = true };
        }
    }
}
