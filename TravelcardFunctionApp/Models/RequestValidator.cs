using System;
using System.Collections.Generic;
using System.Linq;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Models
{
    public class RequestValidator
    {
        public List<string> Validate(TravelcardCreateRequest req)
        {
            var errors = new List<string>();

            // travelcardNumber
            if (string.IsNullOrWhiteSpace(req.TravelcardNumber))
                errors.Add("travelcardNumber is required");
            else
            {
                if (req.TravelcardNumber.Length < 11 || req.TravelcardNumber.Length > 22)
                    errors.Add("travelcardNumber must be between 11 and 22 characters");
                if (!req.TravelcardNumber.All(c => char.IsLetterOrDigit(c)))
                    errors.Add("travelcardNumber must be alphanumeric");
            }

            // transaction reference
            if (string.IsNullOrWhiteSpace(req.TravelcardTransactionReference))
                errors.Add("travelcardTransactionReference is required");
            else if (req.TravelcardTransactionReference.Length != 15)
                errors.Add("travelcardTransactionReference must be exactly 15 characters");

            // dates
            var now = DateTime.UtcNow;
            if (req.TravelcardRequestedDate > now)
                errors.Add("travelcardRequestedDate must be in the past");

            if (req.TravelcardValidFrom > req.TravelcardValidTo)
                errors.Add("travelcardValidFrom must be earlier than or equal to travelcardValidTo");

            if (req.TravelcardValidTo <= now)
                errors.Add("travelcardValidTo must be in the future");

            if (req.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!req.TravelcardUsableTo.HasValue)
                    errors.Add("travelcardUsableTo is required for SixteenToSeventeen travelcards");
                else if (req.TravelcardUsableTo.Value <= now)
                    errors.Add("travelcardUsableTo must be in the future");
            }

            // cardholders: 1 or 2, exactly one Primary
            if (req.Cardholders == null || req.Cardholders.Count < 1 || req.Cardholders.Count > 2)
                errors.Add("cardholders must contain 1 or 2 items");
            else
            {
                var primaries = req.Cardholders.Count(c => c.CardholderType == CardholderType.Primary);
                if (primaries != 1)
                    errors.Add("Exactly one Primary cardholder must be provided");

                // each cardholder must provide exactly one photo identification
                foreach (var ch in req.Cardholders)
                {
                    var provided = 0;
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoRrsKey)) provided++;
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoUrl)) provided++;
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoKey)) provided++;
                    if (provided != 1)
                        errors.Add($"Cardholder {ch.CardholderForename} {ch.CardholderSurname} must provide exactly one of cardholder_photo_rrs_key, cardholder_photo_url, or cardholder_photo_key");

                    if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                        errors.Add("cardholderTitle is required and must be <= 15 characters");
                    if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                        errors.Add("cardholderForename is required and must be <= 100 characters");
                    if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                        errors.Add("cardholderSurname is required and must be <= 100 characters");
                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                        errors.Add("cardholderPhotoName is required and must be <= 100 characters");
                }
            }

            // travelcardName: optional but if provided must match DB check (simple allowed chars)
            if (!string.IsNullOrEmpty(req.TravelcardName))
            {
                foreach (var ch in req.TravelcardName)
                {
                    if (!(char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch)))
                    {
                        errors.Add("travelcardName contains invalid characters");
                        break;
                    }
                }
                if (req.TravelcardName.Length > 255)
                    errors.Add("travelcardName must be <= 255 characters");
            }

            return errors;
        }
    }
}
