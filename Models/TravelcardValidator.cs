namespace travelcardlambdachsarp549.Models;

public static class TravelcardValidator
{
    public static string? Validate(CreateTravelcardRequest request)
    {
        if (request.TravelcardRequestedDate > DateTime.UtcNow) return "travelcardRequestedDate must be in the past";
        if (request.TravelcardValidFrom > DateTime.UtcNow.AddMonths(1)) return "travelcardValidFrom must be no later than one calendar month from today";
        if (request.TravelcardValidFrom > request.TravelcardValidTo) return "travelcardValidFrom must not be later than travelcardValidTo";
        if (request.TravelcardValidTo <= DateTime.UtcNow) return "travelcardValidTo must be in the future";
        if (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && request.TravelcardUsableTo is null) return "travelcardUsableTo is required for SixteenToSeventeen";
        if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTime.UtcNow) return "travelcardUsableTo must be in the future";
        if ((request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen || request.TravelcardType == TravelcardTypeEnum.Veterans) && request.Cardholders.Any(x => x.CardholderType == CardholderTypeEnum.Secondary)) return "Secondary cardholder is not allowed for SixteenToSeventeen and Veterans";
        if (request.Cardholders is null || request.Cardholders.Count < 1 || request.Cardholders.Count > 2) return "cardholders must contain exactly one or two items";
        if (!request.Cardholders.Any(x => x.CardholderType == CardholderTypeEnum.Primary)) return "Exactly one Primary cardholder is required";
        if (request.Cardholders.Count(x => x.CardholderType == CardholderTypeEnum.Primary) != 1) return "Exactly one Primary cardholder is required";
        if (request.Cardholders.Count(x => x.CardholderType == CardholderTypeEnum.Secondary) > 1) return "Only one Secondary cardholder is allowed";
        foreach (var cardholder in request.Cardholders)
        {
            if (string.IsNullOrWhiteSpace(cardholder.CardholderTitle) || cardholder.CardholderTitle.Length > 15) return "cardholderTitle must be 1 to 15 characters";
            if (string.IsNullOrWhiteSpace(cardholder.CardholderForename) || cardholder.CardholderForename.Length > 100) return "cardholderForename must be 1 to 100 characters";
            if (string.IsNullOrWhiteSpace(cardholder.CardholderSurname) || cardholder.CardholderSurname.Length > 100) return "cardholderSurname must be 1 to 100 characters";
            if (string.IsNullOrWhiteSpace(cardholder.CardholderPhotoName) || cardholder.CardholderPhotoName.Length > 100) return "cardholderPhotoName must be 1 to 100 characters";
            var oneOfCount = new[] { cardholder.CardholderPhotoRRSKey, cardholder.CardholderPhotoURL, cardholder.CardholderPhotoKey }.Count(v => !string.IsNullOrWhiteSpace(v));
            if (oneOfCount != 1) return "Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey";
        }
        if (string.IsNullOrWhiteSpace(request.TravelcardNumber) || request.TravelcardNumber.Length < 11 || request.TravelcardNumber.Length > 22) return "travelcardNumber must be 11 to 22 characters";
        if (string.IsNullOrWhiteSpace(request.TravelcardTransactionReference) || request.TravelcardTransactionReference.Length != 15) return "travelcardTransactionReference must be exactly 15 characters";
        return null;
    }
}