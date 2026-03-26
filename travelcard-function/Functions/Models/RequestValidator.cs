using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TravelcardFunction.Models;

namespace TravelcardFunction.Models
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    public static class RequestValidator
    {
        public static ValidationResult Validate(TravelcardCreateRequest req)
        {
            if (req == null) return new ValidationResult { IsValid = false, Error = "Request is null" };

            if (req.TravelcardRequestedDate > DateTime.UtcNow)
                return new ValidationResult { IsValid = false, Error = "Requested date must be in the past" };

            if (req.TravelcardValidFrom > req.TravelcardValidTo)
                return new ValidationResult { IsValid = false, Error = "ValidFrom must be earlier than or equal to ValidTo" };

            if (req.TravelcardValidTo <= DateTime.UtcNow)
                return new ValidationResult { IsValid = false, Error = "ValidTo must be in the future" };

            if (req.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!req.TravelcardUsableTo.HasValue)
                    return new ValidationResult { IsValid = false, Error = "UsableTo is required for SixteenToSeventeen travelcard" };
                if (req.TravelcardUsableTo.Value <= DateTime.UtcNow)
                    return new ValidationResult { IsValid = false, Error = "UsableTo must be in the future" };
            }

            if (string.IsNullOrWhiteSpace(req.TravelcardNumber) || req.TravelcardNumber.Length < 11 || req.TravelcardNumber.Length > 22)
                return new ValidationResult { IsValid = false, Error = "TravelcardNumber must be between 11 and 22 characters" };

            if (string.IsNullOrWhiteSpace(req.TravelcardTransactionReference) || req.TravelcardTransactionReference.Length != 15)
                return new ValidationResult { IsValid = false, Error = "TravelcardTransactionReference must be exactly 15 characters" };

            if (req.TravelcardName != null)
            {
                if (req.TravelcardName.Length > 255) return new ValidationResult { IsValid = false, Error = "TravelcardName exceeds 255 characters" };
                if (!Regex.IsMatch(req.TravelcardName, "^[A-Za-z0-9 ]*$")) return new ValidationResult { IsValid = false, Error = "TravelcardName contains invalid characters" };
            }

            if (req.Cardholders == null || req.Cardholders.Count < 1 || req.Cardholders.Count > 2)
                return new ValidationResult { IsValid = false, Error = "Cardholders must contain exactly 1 or 2 items" };

            int primaryCount = 0;
            foreach (var ch in req.Cardholders)
            {
                if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                    return new ValidationResult { IsValid = false, Error = "CardholderTitle invalid" };
                if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                    return new ValidationResult { IsValid = false, Error = "CardholderForename invalid" };
                if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                    return new ValidationResult { IsValid = false, Error = "CardholderSurname invalid" };
                if (ch.CardholderPhotoName == null || ch.CardholderPhotoName.Length < 1 || ch.CardholderPhotoName.Length > 100)
                    return new ValidationResult { IsValid = false, Error = "CardholderPhotoName invalid" };

                bool hasPhoto = !string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey) || !string.IsNullOrWhiteSpace(ch.CardholderPhotoUrl) || !string.IsNullOrWhiteSpace(ch.CardholderPhotoKey);
                if (!hasPhoto) return new ValidationResult { IsValid = false, Error = "Each cardholder must have one photo field provided" };

                if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey))
                {
                    if (ch.CardholderPhotoRrsKey.Length < 39 || ch.CardholderPhotoRrsKey.Length > 42) return new ValidationResult { IsValid = false, Error = "CardholderPhotoRrsKey invalid length" };
                }

                if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey))
                {
                    if (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42) return new ValidationResult { IsValid = false, Error = "CardholderPhotoKey invalid length" };
                }

                if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoUrl))
                {
                    if (ch.CardholderPhotoUrl.Length < 20 || ch.CardholderPhotoUrl.Length > 2048) return new ValidationResult { IsValid = false, Error = "CardholderPhotoUrl invalid length" };
                    if (!Uri.IsWellFormedUriString(ch.CardholderPhotoUrl, UriKind.Absolute)) return new ValidationResult { IsValid = false, Error = "CardholderPhotoUrl must be a valid URI" };
                }

                if (ch.CardholderType == CardholderType.Primary) primaryCount++;
            }

            if (primaryCount != 1) return new ValidationResult { IsValid = false, Error = "Exactly one Primary cardholder required" };

            // Allow secondary only for TwoTogether and Family types
            if (req.Cardholders.Count == 2)
            {
                if (!(req.TravelcardType == TravelcardType.TwoTogether || req.TravelcardType == TravelcardType.Family))
                    return new ValidationResult { IsValid = false, Error = "Secondary cardholder is not allowed for this travelcard type" };
            }

            return new ValidationResult { IsValid = true };
        }
    }
}
