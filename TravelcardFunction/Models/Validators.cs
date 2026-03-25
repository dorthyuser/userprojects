using System;
using System.Collections.Generic;
using TravelcardFunction.Models;

namespace TravelcardFunction.Models
{
    public static class RequestValidator
    {
        public static List<string> Validate(CreateTravelcardRequest req)
        {
            var errors = new List<string>();
            var now = DateTime.UtcNow;

            if (req.TravelcardRequestedDate.Kind == DateTimeKind.Unspecified)
                req.TravelcardRequestedDate = DateTime.SpecifyKind(req.TravelcardRequestedDate, DateTimeKind.Utc);
            if (req.TravelcardValidFrom.Kind == DateTimeKind.Unspecified)
                req.TravelcardValidFrom = DateTime.SpecifyKind(req.TravelcardValidFrom, DateTimeKind.Utc);
            if (req.TravelcardValidTo.Kind == DateTimeKind.Unspecified)
                req.TravelcardValidTo = DateTime.SpecifyKind(req.TravelcardValidTo, DateTimeKind.Utc);
            if (req.TravelcardUsableTo.HasValue && req.TravelcardUsableTo.Value.Kind == DateTimeKind.Unspecified)
                req.TravelcardUsableTo = DateTime.SpecifyKind(req.TravelcardUsableTo.Value, DateTimeKind.Utc);

            // requested_date is in the past
            if (req.TravelcardRequestedDate > now)
                errors.Add("travelcardRequestedDate must be in the past");

            // valid_from earlier than valid_to
            if (req.TravelcardValidFrom >= req.TravelcardValidTo)
                errors.Add("travelcardValidFrom must be earlier than travelcardValidTo");

            // valid_to must be in the future
            if (req.TravelcardValidTo <= now)
                errors.Add("travelcardValidTo must be in the future");

            // If SixteenToSeventeen, usable_to is required and must be in the future
            if (req.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!req.TravelcardUsableTo.HasValue)
                    errors.Add("travelcardUsableTo is required for SixteenToSeventeen travelcard type");
                else if (req.TravelcardUsableTo.Value <= now)
                    errors.Add("travelcardUsableTo must be in the future");
            }
            else if (req.TravelcardUsableTo.HasValue && req.TravelcardUsableTo.Value <= now)
            {
                errors.Add("travelcardUsableTo must be in the future");
            }

            // travelcardNumber length constraints
            if (string.IsNullOrWhiteSpace(req.TravelcardNumber) || req.TravelcardNumber.Length < 11 || req.TravelcardNumber.Length > 22)
                errors.Add("travelcardNumber must be between 11 and 22 characters");

            // transaction reference length
            if (string.IsNullOrWhiteSpace(req.TravelcardTransactionReference) || req.TravelcardTransactionReference.Length != 15)
                errors.Add("travelcardTransactionReference must be exactly 15 characters");

            // cardholders count and validations
            if (req.Cardholders == null || req.Cardholders.Count < 1 || req.Cardholders.Count > 2)
            {
                errors.Add("cardholders must contain exactly 1 or 2 items");
            }
            else
            {
                int primaryCount = 0;
                foreach (var ch in req.Cardholders)
                {
                    if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                        errors.Add("cardholderTitle is required and must be <= 15 characters");
                    if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                        errors.Add("cardholderForename is required and must be <= 100 characters");
                    if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                        errors.Add("cardholderSurname is required and must be <= 100 characters");
                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                        errors.Add("cardholderPhotoName is required and must be <= 100 characters");

                    if (ch.CardholderType == CardholderType.Primary) primaryCount++;

                    // OneOf photo fields
                    var providedPhotos = 0;
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey)) providedPhotos++;
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoUrl)) providedPhotos++;
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey)) providedPhotos++;
                    if (providedPhotos == 0)
                        errors.Add("Each cardholder must provide one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey");
                    if (providedPhotos > 1)
                        errors.Add("Cardholder must provide only one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey");

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey) && (ch.CardholderPhotoRrsKey.Length < 39 || ch.CardholderPhotoRrsKey.Length > 42))
                        errors.Add("cardholderPhotoRRSKey length invalid");
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) && (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42))
                        errors.Add("cardholderPhotoKey length invalid");
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoUrl) && (ch.CardholderPhotoUrl.Length < 20 || ch.CardholderPhotoUrl.Length > 2048))
                        errors.Add("cardholderPhotoURL length invalid");
                }

                if (primaryCount != 1)
                    errors.Add("There must be exactly one Primary cardholder");
            }

            return errors;
        }
    }

    public static class BusinessRules
    {
        // Define which travelcard types allow a secondary cardholder. Assumption: Family and TwoTogether allow Secondary.
        public static bool SecondaryAllowed(TravelcardType type, List<CardholderDto> cardholders)
        {
            if (cardholders == null) return true;
            var hasSecondary = cardholders.Exists(c => c.CardholderType == CardholderType.Secondary);
            if (!hasSecondary) return true;
            return type == TravelcardType.Family || type == TravelcardType.TwoTogether;
        }
    }
}
