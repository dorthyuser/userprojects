using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers
{
    public static class Validators
    {
        public static List<string> ValidateTravelcardRequest(TravelcardRequest req)
        {
            var errors = new List<string>();
            var now = DateTime.UtcNow;

            // travelcardType required - enum deserialization will have default if missing; check name validity
            if (!Enum.IsDefined(typeof(TravelcardType), req.TravelcardType)) errors.Add("Invalid travelcardType");

            // requested date must be in the past or present
            if (req.TravelcardRequestedDate > now) errors.Add("travelcardRequestedDate must be in the past");

            // valid_from must not be later than valid_to (i.e., validFrom <= validTo)
            if (req.TravelcardValidFrom > req.TravelcardValidTo) errors.Add("travelcardValidFrom must not be later than travelcardValidTo");

            // valid_to must be in the future
            if (req.TravelcardValidTo <= now) errors.Add("travelcardValidTo must be in the future");

            // If SixteenToSeventeen, usableTo is required and must be in the future and after valid_from
            if (req.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!req.TravelcardUsableTo.HasValue) errors.Add("travelcardUsableTo is required for SixteenToSeventeen");
                else
                {
                    if (req.TravelcardUsableTo.Value <= now) errors.Add("travelcardUsableTo must be in the future");
                }
            }
            else
            {
                if (req.TravelcardUsableTo.HasValue && req.TravelcardUsableTo.Value <= now) errors.Add("travelcardUsableTo, if provided, must be in the future");
            }

            // travelcardNumber length checks
            if (string.IsNullOrWhiteSpace(req.TravelcardNumber) || req.TravelcardNumber.Length < 11 || req.TravelcardNumber.Length > 22) errors.Add("travelcardNumber must be between 11 and 22 characters");

            // transaction reference length 15
            if (string.IsNullOrWhiteSpace(req.TravelcardTransactionReference) || req.TravelcardTransactionReference.Length != 15) errors.Add("travelcardTransactionReference must be 15 characters");

            // cardholders validation: 1 or 2; exactly one Primary; if 2, one Primary and one Secondary
            if (req.Cardholders == null || req.Cardholders.Count < 1 || req.Cardholders.Count > 2) errors.Add("cardholders must contain 1 or 2 items");
            else
            {
                var primaryCount = req.Cardholders.Count(c => c.CardholderType == CardholderType.Primary);
                var secondaryCount = req.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary);
                if (primaryCount != 1) errors.Add("Exactly one Primary cardholder is required");
                if (secondaryCount > 1) errors.Add("At most one Secondary cardholder is allowed");

                // Secondary allowed only for certain travelcard types (TwoTogether, Family)
                if (secondaryCount == 1 && !(req.TravelcardType == TravelcardType.TwoTogether || req.TravelcardType == TravelcardType.Family))
                {
                    errors.Add("Secondary cardholder is only allowed for travelcard types: TwoTogether, Family");
                }

                foreach (var ch in req.Cardholders)
                {
                    if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15) errors.Add("cardholderTitle is required and must be <= 15 characters");
                    if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100) errors.Add("cardholderForename is required and must be <= 100 characters");
                    if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100) errors.Add("cardholderSurname is required and must be <= 100 characters");
                    if (!Enum.IsDefined(typeof(CardholderType), ch.CardholderType)) errors.Add("Invalid cardholderType");
                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100) errors.Add("cardholderPhotoName is required and must be <= 100 characters");

                    // Require one of photo fields
                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) && string.IsNullOrWhiteSpace(ch.CardholderPhotoURL) && string.IsNullOrWhiteSpace(ch.CardholderPhotoKey))
                    {
                        errors.Add("Each cardholder must have one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey");
                    }

                    // If provided, validate lengths and patterns
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) && (ch.CardholderPhotoRRSKey.Length < 39 || ch.CardholderPhotoRRSKey.Length > 42)) errors.Add("cardholderPhotoRRSKey must be between 39 and 42 characters");
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) && (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42)) errors.Add("cardholderPhotoKey must be between 39 and 42 characters");
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoURL))
                    {
                        if (ch.CardholderPhotoURL.Length < 20 || ch.CardholderPhotoURL.Length > 2048) errors.Add("cardholderPhotoURL must be between 20 and 2048 characters");
                        else
                        {
                            if (!Uri.IsWellFormedUriString(ch.CardholderPhotoURL, UriKind.Absolute)) errors.Add("cardholderPhotoURL must be a valid URI");
                        }
                    }
                }
            }

            // requestedDate must not be in the future
            if (req.TravelcardRequestedDate > now) errors.Add("travelcardRequestedDate must be in the past");

            // Check travelcardName allowed characters if provided
            if (!string.IsNullOrWhiteSpace(req.TravelcardName))
            {
                var match = Regex.IsMatch(req.TravelcardName, "^[A-Za-z0-9 ]*$");
                if (!match) errors.Add("travelcardName contains invalid characters");
            }

            return errors;
        }
    }
}
