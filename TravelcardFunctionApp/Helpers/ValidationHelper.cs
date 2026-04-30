using System;
using System.Collections.Generic;
using System.Linq;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers
{
    public class ValidationResult
    {
        public List<string> Errors { get; set; } = new List<string>();
    }

    public static class ValidationHelper
    {
        public static ValidationResult ValidateTravelcardRequest(TravelcardRequest req)
        {
            var result = new ValidationResult();
            if (req == null)
            {
                result.Errors.Add("Request body is null.");
                return result;
            }

            // travelcardType required
            if (!Enum.IsDefined(typeof(TravelcardType), req.TravelcardType))
            {
                result.Errors.Add("Invalid travelcardType.");
            }

            // Dates
            var now = DateTime.UtcNow;
            if (req.TravelcardRequestedDate > now)
            {
                result.Errors.Add("travelcardRequestedDate must be in the past.");
            }

            if (req.TravelcardValidFrom >= req.TravelcardValidTo)
            {
                result.Errors.Add("travelcardValidFrom must be earlier than travelcardValidTo.");
            }

            if (req.TravelcardValidTo <= now)
            {
                result.Errors.Add("travelcardValidTo must be in the future.");
            }

            if (req.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!req.TravelcardUsableTo.HasValue)
                {
                    result.Errors.Add("travelcardUsableTo is required for TravelcardType 'SixteenToSeventeen'.");
                }
                else if (req.TravelcardUsableTo.Value <= now)
                {
                    result.Errors.Add("travelcardUsableTo must be in the future.");
                }
            }

            // travelcardNumber length
            if (string.IsNullOrWhiteSpace(req.TravelcardNumber) || req.TravelcardNumber.Length < 11 || req.TravelcardNumber.Length > 22)
            {
                result.Errors.Add("travelcardNumber must be between 11 and 22 characters.");
            }

            // transaction reference length
            if (string.IsNullOrWhiteSpace(req.TravelcardTransactionReference) || req.TravelcardTransactionReference.Length != 15)
            {
                result.Errors.Add("travelcardTransactionReference must be exactly 15 characters.");
            }

            // cardholders 1 or 2
            if (req.Cardholders == null || req.Cardholders.Count < 1 || req.Cardholders.Count > 2)
            {
                result.Errors.Add("cardholders must contain exactly 1 or 2 entries.");
            }
            else
            {
                // Secondary allowed only for specific types (TwoTogether, Family)
                if (req.Cardholders.Count == 2)
                {
                    var allowed = new[] { TravelcardType.TwoTogether, TravelcardType.Family };
                    if (!allowed.Contains(req.TravelcardType))
                    {
                        result.Errors.Add($"Secondary cardholder is not allowed for TravelcardType '{req.TravelcardType}'.");
                    }
                }

                foreach (var ch in req.Cardholders)
                {
                    if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                        result.Errors.Add("cardholderTitle is required and must be <= 15 characters.");

                    if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                        result.Errors.Add("cardholderForename is required and must be <= 100 characters.");

                    if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                        result.Errors.Add("cardholderSurname is required and must be <= 100 characters.");

                    if (!Enum.IsDefined(typeof(CardholderType), ch.CardholderType))
                        result.Errors.Add("cardholderType is invalid.");

                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                        result.Errors.Add("cardholderPhotoName is required and must be <= 100 characters.");

                    // OneOf photo fields
                    var provided = 0;
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey)) provided++;
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoURL)) provided++;
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey)) provided++;
                    if (provided == 0)
                        result.Errors.Add("One of cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key must be provided for each cardholder.");

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) && (ch.CardholderPhotoRRSKey.Length < 39 || ch.CardholderPhotoRRSKey.Length > 42))
                        result.Errors.Add("cardholderPhotoRRSKey must be between 39 and 42 characters if provided.");

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoURL) && (ch.CardholderPhotoURL.Length < 20 || ch.CardholderPhotoURL.Length > 2048))
                        result.Errors.Add("cardholderPhotoURL must be between 20 and 2048 characters if provided.");

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) && (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42))
                        result.Errors.Add("cardholderPhotoKey must be between 39 and 42 characters if provided.");
                }
            }

            // travelcardName length
            if (!string.IsNullOrWhiteSpace(req.TravelcardName) && req.TravelcardName.Length > 255)
                result.Errors.Add("travelcardName must be <= 255 characters.");

            return result;
        }
    }
}
