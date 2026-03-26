using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TravelcardApi.Models
{
    public static class Validators
    {
        public static void Validate(TravelcardRequest req)
        {
            var errors = new List<string>();

            // requested_date is in the past
            if (req.travelcardRequestedDate > DateTime.UtcNow.AddMinutes(1))
                errors.Add("travelcardRequestedDate must be in the past");

            // valid_from must be earlier than valid_to
            if (req.travelcardValidFrom > req.travelcardValidTo)
                errors.Add("travelcardValidFrom must be earlier than or equal to travelcardValidTo");

            // valid_to must be in the future
            if (req.travelcardValidTo <= DateTime.UtcNow)
                errors.Add("travelcardValidTo must be in the future");

            // If SixteenToSeventeen then usable_to is required and in future
            if (req.travelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!req.travelcardUsableTo.HasValue)
                    errors.Add("travelcardUsableTo is required for SixteenToSeventeen travelcards");
                else if (req.travelcardUsableTo.Value <= DateTime.UtcNow)
                    errors.Add("travelcardUsableTo must be in the future");
            }

            // travelcardName length
            if (!string.IsNullOrEmpty(req.travelcardName) && req.travelcardName.Length > 255)
                errors.Add("travelcardName exceeds 255 characters");

            // travelcardNumber length and pattern
            if (string.IsNullOrEmpty(req.travelcardNumber) || req.travelcardNumber.Length < 11 || req.travelcardNumber.Length > 22)
                errors.Add("travelcardNumber must be between 11 and 22 characters");

            if (!Regex.IsMatch(req.travelcardNumber ?? string.Empty, "^[A-Za-z0-9]+$"))
                errors.Add("travelcardNumber must be alphanumeric");

            // transaction reference length
            if (string.IsNullOrEmpty(req.travelcardTransactionReference) || req.travelcardTransactionReference.Length != 15)
                errors.Add("travelcardTransactionReference must be exactly 15 characters");

            // cardholders count 1 or 2 and must include Primary
            if (req.cardholders == null || req.cardholders.Count < 1 || req.cardholders.Count > 2)
                errors.Add("cardholders must contain 1 or 2 items");
            else
            {
                var hasPrimary = false;
                foreach (var ch in req.cardholders)
                {
                    if (ch.cardholderType == CardholderType.Primary) hasPrimary = true;
                    if (string.IsNullOrWhiteSpace(ch.cardholderTitle) || ch.cardholderTitle.Length > 15)
                        errors.Add("cardholderTitle is required and must be <= 15 characters");
                    if (string.IsNullOrWhiteSpace(ch.cardholderForename) || ch.cardholderForename.Length > 100)
                        errors.Add("cardholderForename is required and must be <= 100 characters");
                    if (string.IsNullOrWhiteSpace(ch.cardholderSurname) || ch.cardholderSurname.Length > 100)
                        errors.Add("cardholderSurname is required and must be <= 100 characters");
                    if (string.IsNullOrWhiteSpace(ch.cardholderPhotoName) || ch.cardholderPhotoName.Length > 100)
                        errors.Add("cardholderPhotoName is required and must be <= 100 characters");

                    // oneOf photo fields
                    var provided = 0;
                    if (!string.IsNullOrEmpty(ch.cardholderPhotoRRSKey)) provided++;
                    if (!string.IsNullOrEmpty(ch.cardholderPhotoURL)) provided++;
                    if (!string.IsNullOrEmpty(ch.cardholderPhotoKey)) provided++;
                    if (provided == 0)
                        errors.Add("Each cardholder must include one of cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey");

                    if (!string.IsNullOrEmpty(ch.cardholderPhotoRRSKey) && (ch.cardholderPhotoRRSKey.Length < 39 || ch.cardholderPhotoRRSKey.Length > 42))
                        errors.Add("cardholderPhotoRRSKey must be between 39 and 42 characters when provided");
                    if (!string.IsNullOrEmpty(ch.cardholderPhotoKey) && (ch.cardholderPhotoKey.Length < 39 || ch.cardholderPhotoKey.Length > 42))
                        errors.Add("cardholderPhotoKey must be between 39 and 42 characters when provided");
                    if (!string.IsNullOrEmpty(ch.cardholderPhotoURL) && (ch.cardholderPhotoURL.Length < 20 || ch.cardholderPhotoURL.Length > 2048))
                        errors.Add("cardholderPhotoURL length must be between 20 and 2048 when provided");

                    // basic name regex checks
                    var namePattern = "^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\\-]+$";
                    if (!Regex.IsMatch(ch.cardholderForename, namePattern))
                        errors.Add("cardholderForename contains invalid characters");
                    if (!Regex.IsMatch(ch.cardholderSurname, namePattern))
                        errors.Add("cardholderSurname contains invalid characters");
                }

                if (!hasPrimary)
                    errors.Add("At least one Primary cardholder is required");

                // Secondary allowed only for certain travelcard types - business rule: allow only Family and TwoTogether
                if (req.cardholders.Count == 2)
                {
                    if (req.travelcardType != TravelcardType.Family && req.travelcardType != TravelcardType.TwoTogether)
                        errors.Add("Secondary cardholder is not allowed for this travelcard type");
                }
            }

            if (errors.Count > 0)
                throw new ValidationException(errors);
        }
    }
}
