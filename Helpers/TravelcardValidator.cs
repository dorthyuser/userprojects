namespace azurecsharppost1112.Helpers;

public static class TravelcardValidator
{
    public static string? Validate(Models.CreateTravelcardRequest request)
    {
        if (request.travelcardRequestedDate >= DateTime.UtcNow)
        {
            return "travelcardRequestedDate must be in the past.";
        }

        if (request.travelcardValidFrom > DateTime.UtcNow.AddMonths(1))
        {
            return "travelcardValidFrom must be no later than one calendar month from today.";
        }

        if (request.travelcardValidFrom > request.travelcardValidTo)
        {
            return "travelcardValidFrom cannot be later than travelcardValidTo.";
        }

        if (request.travelcardValidTo <= DateTime.UtcNow)
        {
            return "travelcardValidTo must be in the future.";
        }

        if (request.travelcardType == travelcardType_enum.SixteenToSeventeen && request.travelcardUsableTo is null)
        {
            return "travelcardUsableTo is required for SixteenToSeventeen travelcard type.";
        }

        if (request.travelcardUsableTo is not null && request.travelcardUsableTo <= DateTime.UtcNow)
        {
            return "travelcardUsableTo must be in the future.";
        }

        if ((request.travelcardType == travelcardType_enum.SixteenToSeventeen || request.travelcardType == travelcardType_enum.Veterans) && request.cardholders.Any(c => c.cardholderType == cardholderType_enum.Secondary))
        {
            return "Secondary cardholder is not allowed for SixteenToSeventeen and Veterans travelcard types.";
        }

        if (request.cardholders.Count is < 1 or > 2)
        {
            return "cardholders must contain exactly 1 or 2 items.";
        }

        if (!request.cardholders.Any(c => c.cardholderType == cardholderType_enum.Primary))
        {
            return "Exactly one Primary cardholder is required.";
        }

        if (request.cardholders.Count(c => c.cardholderType == cardholderType_enum.Primary) != 1)
        {
            return "Exactly one Primary cardholder is required.";
        }

        foreach (var cardholder in request.cardholders)
        {
            if (string.IsNullOrWhiteSpace(cardholder.cardholderTitle) || cardholder.cardholderTitle.Length > 15) return "Invalid cardholderTitle.";
            if (string.IsNullOrWhiteSpace(cardholder.cardholderForename) || cardholder.cardholderForename.Length > 100) return "Invalid cardholderForename.";
            if (string.IsNullOrWhiteSpace(cardholder.cardholderSurname) || cardholder.cardholderSurname.Length > 100) return "Invalid cardholderSurname.";
            if (string.IsNullOrWhiteSpace(cardholder.cardholderPhotoName) || cardholder.cardholderPhotoName.Length > 100) return "Invalid cardholderPhotoName.";
            if (!string.IsNullOrEmpty(cardholder.cardholderPhotoRRSKey) && (cardholder.cardholderPhotoRRSKey.Length < 39 || cardholder.cardholderPhotoRRSKey.Length > 42)) return "Invalid cardholderPhotoRRSKey.";
            if (!string.IsNullOrEmpty(cardholder.cardholderPhotoURL) && (cardholder.cardholderPhotoURL.Length < 20 || cardholder.cardholderPhotoURL.Length > 2048 || !Uri.TryCreate(cardholder.cardholderPhotoURL, UriKind.Absolute, out _))) return "Invalid cardholderPhotoURL.";
            if (!string.IsNullOrEmpty(cardholder.cardholderPhotoKey) && (cardholder.cardholderPhotoKey.Length < 39 || cardholder.cardholderPhotoKey.Length > 42)) return "Invalid cardholderPhotoKey.";
            var oneOfCount = new[] { cardholder.cardholderPhotoRRSKey, cardholder.cardholderPhotoURL, cardholder.cardholderPhotoKey }.Count(v => !string.IsNullOrWhiteSpace(v));
            if (oneOfCount != 1) return "Exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey is required for each cardholder.";
        }

        if (string.IsNullOrWhiteSpace(request.travelcardNumber) || request.travelcardNumber.Length < 11 || request.travelcardNumber.Length > 22) return "Invalid travelcardNumber.";
        if (string.IsNullOrWhiteSpace(request.travelcardTransactionReference) || request.travelcardTransactionReference.Length != 15) return "Invalid travelcardTransactionReference.";

        return null;
    }
}
