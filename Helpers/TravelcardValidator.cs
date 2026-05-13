using System;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers;

public static class TravelcardValidator
{
    public static string? Validate(CreateTravelcardRequest request)
    {
        if (request is null) return "Request is required.";
        if (!Enum.IsDefined(typeof(TravelcardTypeEnum), request.TravelcardType)) return "travelcardType is invalid.";
        if (request.Cardholders is null || request.Cardholders.Count < 1 || request.Cardholders.Count > 2) return "cardholders must contain exactly one Primary and optionally one Secondary.";
        if (request.TravelcardRequestedDate >= DateTime.UtcNow) return "travelcardRequestedDate must be in the past.";
        if (request.TravelcardValidFrom > DateTime.UtcNow.AddMonths(1)) return "travelcardValidFrom must be no later than one calendar month from today.";
        if (request.TravelcardValidFrom > request.TravelcardValidTo) return "travelcardValidFrom must not be later than travelcardValidTo.";
        if (request.TravelcardValidTo <= DateTime.UtcNow) return "travelcardValidTo must be in the future.";
        if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTime.UtcNow) return "travelcardUsableTo must be in the future.";
        if (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && !request.TravelcardUsableTo.HasValue) return "travelcardUsableTo is required for SixteenToSeventeen travelcard type.";
        if ((request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen || request.TravelcardType == TravelcardTypeEnum.Veterans) && request.Cardholders.Exists(c => c.CardholderType == CardholderTypeEnum.Secondary)) return "Secondary cardholder is not allowed for SixteenToSeventeen or Veterans travelcard types.";

        var primaryCount = 0;
        var secondaryCount = 0;
        foreach (var cardholder in request.Cardholders)
        {
            if (cardholder.CardholderType == CardholderTypeEnum.Primary) primaryCount++;
            if (cardholder.CardholderType == CardholderTypeEnum.Secondary) secondaryCount++;

            if (string.IsNullOrWhiteSpace(cardholder.CardholderTitle) || cardholder.CardholderTitle.Length > 15) return "cardholderTitle is invalid.";
            if (string.IsNullOrWhiteSpace(cardholder.CardholderForename) || cardholder.CardholderForename.Length > 100) return "cardholderForename is invalid.";
            if (string.IsNullOrWhiteSpace(cardholder.CardholderSurname) || cardholder.CardholderSurname.Length > 100) return "cardholderSurname is invalid.";
            if (string.IsNullOrWhiteSpace(cardholder.CardholderPhotoName) || cardholder.CardholderPhotoName.Length > 100) return "cardholderPhotoName is invalid.";

            var provided = 0;
            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoRRSKey)) provided++;
            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoURL)) provided++;
            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoKey)) provided++;
            if (provided != 1) return "Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey.";
        }

        if (primaryCount != 1) return "Exactly one Primary cardholder is required.";
        if (secondaryCount > 1) return "Only one Secondary cardholder is allowed.";
        return null;
    }
}