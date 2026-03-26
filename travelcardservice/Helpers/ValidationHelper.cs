using System;
using System.Linq;
using travelcardservice.Models;

namespace travelcardservice.Helpers
{
    public static class ValidationHelper
    {
        public static (bool IsValid, string ErrorMessage) ValidateTravelcardRequest(TravelcardRequest req)
        {
            // requested_date in the past
            if (req.TravelcardRequestedDate >= DateTime.UtcNow.AddSeconds(1))
            {
                return (false, "travelcardRequestedDate must be in the past.");
            }

            // valid_from must be earlier than valid_to
            if (req.TravelcardValidFrom >= req.TravelcardValidTo)
            {
                return (false, "travelcardValidFrom must be earlier than travelcardValidTo.");
            }

            // valid_to in the future
            if (req.TravelcardValidTo <= DateTime.UtcNow)
            {
                return (false, "travelcardValidTo must be in the future.");
            }

            // valid_from no later than one calendar month from creation (use requested date as creation proxy)
            if (req.TravelcardValidFrom > DateTime.UtcNow.AddMonths(1))
            {
                return (false, "travelcardValidFrom must be no later than one calendar month from now.");
            }

            // usable_to required only for SixteenToSeventeen
            if (req.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!req.TravelcardUsableTo.HasValue)
                {
                    return (false, "travelcardUsableTo is required for SixteenToSeventeen travelcards.");
                }
                if (req.TravelcardUsableTo.Value <= DateTime.UtcNow)
                {
                    return (false, "travelcardUsableTo must be in the future.");
                }
            }

            // If provided usable_to must be in future
            if (req.TravelcardUsableTo.HasValue && req.TravelcardUsableTo.Value <= DateTime.UtcNow)
            {
                return (false, "travelcardUsableTo must be in the future.");
            }

            // Cardholders count 1 or 2
            if (req.Cardholders == null || req.Cardholders.Length < 1 || req.Cardholders.Length > 2)
            {
                return (false, "cardholders must contain exactly 1 or 2 items.");
            }

            // Exactly one Primary
            var primaryCount = req.Cardholders.Count(c => c.CardholderType == CardholderType.Primary);
            if (primaryCount != 1)
            {
                return (false, "There must be exactly one Primary cardholder.");
            }

            // Secondary allowed only for certain travelcard types (TwoTogether, Family)
            var hasSecondary = req.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary);
            if (hasSecondary)
            {
                if (!(req.TravelcardType == TravelcardType.TwoTogether || req.TravelcardType == TravelcardType.Family))
                {
                    return (false, "Secondary cardholder is only allowed for TwoTogether and Family travelcard types.");
                }
            }

            // Validate cardholder photo fields - require one of three
            foreach (var ch in req.Cardholders)
            {
                var hasPhoto = !string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) || !string.IsNullOrWhiteSpace(ch.CardholderPhotoURL) || !string.IsNullOrWhiteSpace(ch.CardholderPhotoKey);
                if (!hasPhoto)
                {
                    return (false, $"Cardholder {ch.CardholderForename} {ch.CardholderSurname} must provide one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey.");
                }

                if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) && (ch.CardholderPhotoRRSKey.Length < 39 || ch.CardholderPhotoRRSKey.Length > 42))
                {
                    return (false, "cardholderPhotoRRSKey length must be between 39 and 42 characters.");
                }

                if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) && (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42))
                {
                    return (false, "cardholderPhotoKey length must be between 39 and 42 characters.");
                }

                if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoURL) && (ch.CardholderPhotoURL.Length < 20 || ch.CardholderPhotoURL.Length > 2048))
                {
                    return (false, "cardholderPhotoURL length must be between 20 and 2048 characters.");
                }

                // Basic length checks for title/forename/surname/photo name
                if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                {
                    return (false, "cardholderTitle is required and must be <= 15 characters.");
                }
                if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                {
                    return (false, "cardholderForename is required and must be <= 100 characters.");
                }
                if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                {
                    return (false, "cardholderSurname is required and must be <= 100 characters.");
                }
                if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                {
                    return (false, "cardholderPhotoName is required and must be <= 100 characters.");
                }
            }

            // travelcardNumber length
            if (string.IsNullOrWhiteSpace(req.TravelcardNumber) || req.TravelcardNumber.Length < 11 || req.TravelcardNumber.Length > 22)
            {
                return (false, "travelcardNumber is required and must be between 11 and 22 characters.");
            }

            // transaction reference length 15
            if (string.IsNullOrWhiteSpace(req.TravelcardTransactionReference) || req.TravelcardTransactionReference.Length != 15)
            {
                return (false, "travelcardTransactionReference is required and must be exactly 15 characters.");
            }

            // travelcardName length
            if (!string.IsNullOrWhiteSpace(req.TravelcardName) && req.TravelcardName.Length > 255)
            {
                return (false, "travelcardName must be <= 255 characters.");
            }

            return (true, string.Empty);
        }
    }
}
