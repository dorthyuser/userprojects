using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TravelcardFunction.Models;

namespace TravelcardFunction.Models
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class ValidationService
    {
        public ValidationResult ValidateTravelcardRequest(TravelcardRequest req)
        {
            var result = new ValidationResult { IsValid = true };
            var now = DateTime.UtcNow;

            // requested_date is in the past
            if (req.TravelcardRequestedDate > now)
            {
                result.IsValid = false;
                result.Errors.Add("travelcardRequestedDate must be in the past");
            }

            // valid_from must be earlier than valid_to
            if (!(req.TravelcardValidFrom < req.TravelcardValidTo))
            {
                result.IsValid = false;
                result.Errors.Add("travelcardValidFrom must be earlier than travelcardValidTo");
            }

            // valid_to must be in the future
            if (req.TravelcardValidTo <= now)
            {
                result.IsValid = false;
                result.Errors.Add("travelcardValidTo must be in the future");
            }

            // usable_to required only for SixteenToSeventeen
            if (req.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!req.TravelcardUsableTo.HasValue)
                {
                    result.IsValid = false;
                    result.Errors.Add("travelcardUsableTo is required for SixteenToSeventeen travelcard type");
                }
                else if (req.TravelcardUsableTo.Value <= now)
                {
                    result.IsValid = false;
                    result.Errors.Add("travelcardUsableTo must be in the future");
                }
            }
            else
            {
                if (req.TravelcardUsableTo.HasValue && req.TravelcardUsableTo.Value <= now)
                {
                    result.IsValid = false;
                    result.Errors.Add("travelcardUsableTo, if provided, must be in the future");
                }
            }

            // travelcardNumber length & pattern
            if (string.IsNullOrWhiteSpace(req.TravelcardNumber) || req.TravelcardNumber.Length < 11 || req.TravelcardNumber.Length > 22)
            {
                result.IsValid = false;
                result.Errors.Add("travelcardNumber must be between 11 and 22 characters");
            }
            else if (!Regex.IsMatch(req.TravelcardNumber, "^[A-Za-z0-9]+$"))
            {
                result.IsValid = false;
                result.Errors.Add("travelcardNumber must be alphanumeric");
            }

            if (!string.IsNullOrEmpty(req.TravelcardName) && req.TravelcardName.Length > 255)
            {
                result.IsValid = false;
                result.Errors.Add("travelcardName must be <= 255 characters");
            }
            else if (!string.IsNullOrEmpty(req.TravelcardName) && !Regex.IsMatch(req.TravelcardName, "^[A-Za-z0-9 ]*$"))
            {
                result.IsValid = false;
                result.Errors.Add("travelcardName contains invalid characters");
            }

            // transaction reference length
            if (string.IsNullOrWhiteSpace(req.TravelcardTransactionReference) || req.TravelcardTransactionReference.Length != 15)
            {
                result.IsValid = false;
                result.Errors.Add("travelcardTransactionReference must be exactly 15 characters");
            }

            // cardholders count 1 or 2 and exactly one Primary
            if (req.Cardholders == null || req.Cardholders.Count < 1 || req.Cardholders.Count > 2)
            {
                result.IsValid = false;
                result.Errors.Add("cardholders must contain 1 or 2 items");
            }
            else
            {
                int primaryCount = 0;
                foreach (var ch in req.Cardholders)
                {
                    if (ch.CardholderType == CardholderType.Primary) primaryCount++;

                    if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                    {
                        result.IsValid = false;
                        result.Errors.Add("cardholderTitle is required and must be <= 15 characters");
                    }
                    if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                    {
                        result.IsValid = false;
                        result.Errors.Add("cardholderForename is required and must be <= 100 characters");
                    }
                    if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                    {
                        result.IsValid = false;
                        result.Errors.Add("cardholderSurname is required and must be <= 100 characters");
                    }
                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                    {
                        result.IsValid = false;
                        result.Errors.Add("cardholderPhotoName is required and must be <= 100 characters");
                    }

                    // OneOf photo fields: at least one provided and each must meet its constraints
                    int provided = 0;
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey))
                    {
                        provided++;
                        if (ch.CardholderPhotoRrsKey.Length < 39 || ch.CardholderPhotoRrsKey.Length > 42 || !Regex.IsMatch(ch.CardholderPhotoRrsKey, "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$"))
                        {
                            result.IsValid = false;
                            result.Errors.Add("cardholderPhotoRrsKey invalid format or length");
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoURL))
                    {
                        provided++;
                        if (ch.CardholderPhotoURL.Length < 20 || ch.CardholderPhotoURL.Length > 2048 || !Regex.IsMatch(ch.CardholderPhotoURL, "^(https?://)[A-Za-z0-9._~:/?#@!$&'()*+,;=%-]+$"))
                        {
                            result.IsValid = false;
                            result.Errors.Add("cardholderPhotoURL invalid format or length");
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey))
                    {
                        provided++;
                        if (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42 || !Regex.IsMatch(ch.CardholderPhotoKey, "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$"))
                        {
                            result.IsValid = false;
                            result.Errors.Add("cardholderPhotoKey invalid format or length");
                        }
                    }
                    if (provided == 0)
                    {
                        result.IsValid = false;
                        result.Errors.Add("Each cardholder must provide one of cardholderPhotoRrsKey, cardholderPhotoURL, or cardholderPhotoKey");
                    }
                }

                if (primaryCount != 1)
                {
                    result.IsValid = false;
                    result.Errors.Add("There must be exactly one Primary cardholder");
                }

                // Secondary cardholder allowed only as second cardholder; no additional business rules given, so we accept one optional secondary
            }

            return result;
        }
    }
}
