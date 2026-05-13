using System;
using System.Collections.Generic;
using System.Linq;
using travelcardlambdachsarp549.Models;

namespace travelcardlambdachsarp549.Helpers
{
    public static class RequestValidator
    {
        public static string? Validate(CreateTravelcardRequest request)
        {
            if (request.TravelcardRequestedDate >= DateTime.UtcNow)
            {
                return "travelcardRequestedDate must be in the past.";
            }

            if (request.TravelcardValidFrom > request.TravelcardValidTo)
            {
                return "travelcardValidFrom cannot be later than travelcardValidTo.";
            }

            if (request.TravelcardValidTo <= DateTime.UtcNow)
            {
                return "travelcardValidTo must be in the future.";
            }

            if (request.TravelcardValidFrom > DateTime.UtcNow.AddMonths(1))
            {
                return "travelcardValidFrom must not be later than one calendar month from today.";
            }

            bool hasSecondary = request.Cardholders.Any(c => c.CardholderType == cardholder_type_enum.Secondary);
            if (hasSecondary && (request.TravelcardType == travelcard_type_enum.SixteenToSeventeen || request.TravelcardType == travelcard_type_enum.Veterans))
            {
                return "Secondary cardholder is not allowed for SixteenToSeventeen and Veterans travelcard types.";
            }

            if (request.TravelcardType == travelcard_type_enum.SixteenToSeventeen)
            {
                if (!request.TravelcardUsableTo.HasValue)
                {
                    return "travelcardUsableTo is required for SixteenToSeventeen travelcard type.";
                }
            }

            if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTime.UtcNow)
            {
                return "travelcardUsableTo must be in the future.";
            }

            if (request.Cardholders == null || request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
            {
                return "cardholders must contain exactly one or two items.";
            }

            if (request.Cardholders.Count(c => c.CardholderType == cardholder_type_enum.Primary) != 1)
            {
                return "Exactly one Primary cardholder is required.";
            }

            foreach (var cardholder in request.Cardholders)
            {
                if (string.IsNullOrWhiteSpace(cardholder.CardholderTitle) || cardholder.CardholderTitle.Length > 15)
                {
                    return "cardholderTitle must be 1 to 15 characters.";
                }

                if (string.IsNullOrWhiteSpace(cardholder.CardholderForename) || cardholder.CardholderForename.Length > 100)
                {
                    return "cardholderForename must be 1 to 100 characters.";
                }

                if (string.IsNullOrWhiteSpace(cardholder.CardholderSurname) || cardholder.CardholderSurname.Length > 100)
                {
                    return "cardholderSurname must be 1 to 100 characters.";
                }

                if (string.IsNullOrWhiteSpace(cardholder.CardholderPhotoName) || cardholder.CardholderPhotoName.Length > 100)
                {
                    return "cardholderPhotoName must be 1 to 100 characters.";
                }

                bool hasRrs = !string.IsNullOrWhiteSpace(cardholder.CardholderPhotoRRSKey);
                bool hasUrl = !string.IsNullOrWhiteSpace(cardholder.CardholderPhotoURL);
                bool hasKey = !string.IsNullOrWhiteSpace(cardholder.CardholderPhotoKey);
                if (new[] { hasRrs, hasUrl, hasKey }.Count(x => x) != 1)
                {
                    return "Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey.";
                }
            }

            if (string.IsNullOrWhiteSpace(request.TravelcardNumber) || request.TravelcardNumber.Length < 11 || request.TravelcardNumber.Length > 22)
            {
                return "travelcardNumber must be 11 to 22 characters.";
            }

            if (string.IsNullOrWhiteSpace(request.TravelcardTransactionReference) || request.TravelcardTransactionReference.Length != 15)
            {
                return "travelcardTransactionReference must be exactly 15 characters.";
            }

            return null;
        }
    }
}