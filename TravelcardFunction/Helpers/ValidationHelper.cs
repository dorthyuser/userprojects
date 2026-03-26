using System;
using System.Text.RegularExpressions;
using TravelcardFunction.Models;
using System.Collections.Generic;

namespace TravelcardFunction.Helpers
{
    public static class ValidationHelper
    {
        public static (bool IsValid, string ErrorMessage) ValidateCreateRequest(TravelcardCreateRequest req)
        {
            if (req.TravelcardType == null)
            {
                return (false, "travelcardType is required");
            }

            if (req.TravelcardRequestedDate > DateTime.UtcNow)
            {
                return (false, "travelcardRequestedDate must be in the past");
            }

            // valid_from must be earlier than or equal to valid_to
            if (req.TravelcardValidFrom > req.TravelcardValidTo)
            {
                return (false, "travelcardValidFrom must be earlier than or equal to travelcardValidTo");
            }

            if (req.TravelcardValidTo <= DateTime.UtcNow)
            {
                return (false, "travelcardValidTo must be in the future");
            }

            if (req.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!req.TravelcardUsableTo.HasValue)
                {
                    return (false, "travelcardUsableTo is required for SixteenToSeventeen travelcardType");
                }
                if (req.TravelcardUsableTo <= DateTime.UtcNow)
                {
                    return (false, "travelcardUsableTo must be in the future");
                }
            }

            if (string.IsNullOrWhiteSpace(req.TravelcardNumber) || req.TravelcardNumber.Length < 11 || req.TravelcardNumber.Length > 22)
            {
                return (false, "travelcardNumber must be between 11 and 22 characters");
            }

            if (req.TravelcardTransactionReference == null || req.TravelcardTransactionReference.Length != 15)
            {
                return (false, "travelcardTransactionReference must be exactly 15 characters");
            }

            if (req.Cardholders == null || req.Cardholders.Count < 1 || req.Cardholders.Count > 2)
            {
                return (false, "cardholders must contain 1 or 2 items");
            }

            var primaryCount = 0;
            foreach (var ch in req.Cardholders)
            {
                if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                    return (false, "cardholderTitle is required and must be <= 15 characters");
                if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                    return (false, "cardholderForename is required and must be <= 100 characters");
                if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                    return (false, "cardholderSurname is required and must be <= 100 characters");
                if (ch.CardholderType == null)
                    return (false, "cardholderType is required");
                if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                    return (false, "cardholderPhotoName is required and must be <= 100 characters");

                // OneOf photo fields
                var photoFields = 0;
                if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey)) photoFields++;
                if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoUrl)) photoFields++;
                if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey)) photoFields++;
                if (photoFields != 1)
                    return (false, "each cardholder must provide exactly one of cardholderPhotoRrsKey, cardholderPhotoUrl or cardholderPhotoKey");

                if (ch.CardholderPhotoRrsKey != null && (ch.CardholderPhotoRrsKey.Length < 39 || ch.CardholderPhotoRrsKey.Length > 42))
                    return (false, "cardholderPhotoRrsKey must be between 39 and 42 characters when provided");

                if (ch.CardholderPhotoKey != null && (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42))
                    return (false, "cardholderPhotoKey must be between 39 and 42 characters when provided");

                if (ch.CardholderPhotoUrl != null && (ch.CardholderPhotoUrl.Length < 20 || ch.CardholderPhotoUrl.Length > 2048))
                    return (false, "cardholderPhotoUrl must be between 20 and 2048 characters when provided");

                if (ch.CardholderType == CardholderType.Primary) primaryCount++;
            }

            if (primaryCount != 1) return (false, "exactly one Primary cardholder is required");

            return (true, string.Empty);
        }
    }
}
