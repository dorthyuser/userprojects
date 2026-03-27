using System;
using System.Collections.Generic;
using System.Linq;
using TravelcardService.Models;

namespace TravelcardService.Helpers
{
    public class Validator
    {
        public Validator()
        {
        }

        public List<string> Validate(TravelcardRequest req)
        {
            var errors = new List<string>();
            var now = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(req.TravelcardNumber) || req.TravelcardNumber.Length < 11 || req.TravelcardNumber.Length > 22)
            {
                errors.Add("travelcardNumber must be between 11 and 22 characters");
            }

            if (req.TravelcardRequestedDate == default)
            {
                errors.Add("travelcardRequestedDate is required and must be a valid date-time");
            }
            else if (req.TravelcardRequestedDate > now)
            {
                errors.Add("travelcardRequestedDate must be in the past");
            }

            if (req.TravelcardValidFrom == default || req.TravelcardValidTo == default)
            {
                errors.Add("travelcardValidFrom and travelcardValidTo are required and must be valid date-times");
            }
            else
            {
                if (req.TravelcardValidFrom > req.TravelcardValidTo)
                {
                    errors.Add("travelcardValidFrom must be earlier than or equal to travelcardValidTo");
                }
            }

            if (req.TravelcardValidTo <= now)
            {
                errors.Add("travelcardValidTo must be in the future");
            }

            if (req.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (req.TravelcardUsableTo == null || req.TravelcardUsableTo == default)
                {
                    errors.Add("travelcardUsableTo is required for TravelcardType 'SixteenToSeventeen'");
                }
                else if (req.TravelcardUsableTo <= now)
                {
                    errors.Add("travelcardUsableTo must be in the future");
                }
            }
            else if (req.TravelcardUsableTo != null && req.TravelcardUsableTo != default && req.TravelcardUsableTo <= now)
            {
                errors.Add("travelcardUsableTo, if provided, must be in the future");
            }

            if (string.IsNullOrWhiteSpace(req.TravelcardTransactionReference) || req.TravelcardTransactionReference.Length != 15)
            {
                errors.Add("travelcardTransactionReference is required and must be exactly 15 characters");
            }

            // cardholders
            if (req.Cardholders == null || req.Cardholders.Count < 1 || req.Cardholders.Count > 2)
            {
                errors.Add("cardholders must contain exactly 1 or 2 items");
            }
            else
            {
                var primaryCount = req.Cardholders.Count(c => c.CardholderType == CardholderType.Primary);
                if (primaryCount != 1)
                {
                    errors.Add("There must be exactly one Primary cardholder");
                }

                var secondaryCount = req.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary);
                if (secondaryCount > 1)
                {
                    errors.Add("At most one Secondary cardholder is allowed");
                }

                // Business rule: only certain travelcard types allow Secondary
                var allowedSecondaryTypes = new[] { TravelcardType.Family, TravelcardType.TwoTogether, TravelcardType.Network };
                if (secondaryCount == 1 && !allowedSecondaryTypes.Contains(req.TravelcardType))
                {
                    errors.Add("Secondary cardholder is not allowed for this TravelcardType");
                }

                for (int i = 0; i < req.Cardholders.Count; i++)
                {
                    var ch = req.Cardholders[i];
                    if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                        errors.Add($"cardholders[{i}].cardholderTitle is required and must be 1-15 characters");
                    if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                        errors.Add($"cardholders[{i}].cardholderForename is required and must be 1-100 characters");
                    if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                        errors.Add($"cardholders[{i}].cardholderSurname is required and must be 1-100 characters");
                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                        errors.Add($"cardholders[{i}].cardholderPhotoName is required and must be 1-100 characters");

                    // oneOf photo fields
                    var providedPhotos = 0;
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey)) providedPhotos++;
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoUrl)) providedPhotos++;
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey)) providedPhotos++;
                    if (providedPhotos != 1)
                        errors.Add($"cardholders[{i}] must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey");

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey) && (ch.CardholderPhotoRrsKey.Length < 39 || ch.CardholderPhotoRrsKey.Length > 42))
                        errors.Add($"cardholders[{i}].cardholderPhotoRRSKey must be 39-42 characters when provided");
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) && (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42))
                        errors.Add($"cardholders[{i}].cardholderPhotoKey must be 39-42 characters when provided");
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoUrl) && (ch.CardholderPhotoUrl.Length < 20 || ch.CardholderPhotoUrl.Length > 2048))
                        errors.Add($"cardholders[{i}].cardholderPhotoURL must be 20-2048 characters when provided");
                }
            }

            if (!string.IsNullOrWhiteSpace(req.TravelcardName) && req.TravelcardName.Length > 255)
            {
                errors.Add("travelcardName must be 0-255 characters");
            }

            return errors;
        }

        public string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rnd = new Random();
            var tokenChars = new char[length];
            for (int i = 0; i < length; i++) tokenChars[i] = chars[rnd.Next(chars.Length)];
            return new string(tokenChars);
        }
    }
}
