using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.Lambda.APIGatewayEvents;
using Travelcardchsarplambda1050Lambda.Models;

namespace Travelcardchsarplambda1050Lambda.Services;

public static class RequestValidator
{
    public static ValidationError? Validate(APIGatewayProxyRequest request)
    {
        if (request.Headers == null || !request.Headers.TryGetValue("client_id", out var clientId) || string.IsNullOrWhiteSpace(clientId))
            return new ValidationError("client_id", "required.", "client_id is required.");

        if (request.Body is null)
            return new ValidationError("body", "required.", "Request body is required.");

        var model = JsonSerializer.Deserialize<CreateTravelcardRequest>(request.Body, JsonOptionsFactory.CreateOptions());
        if (model is null)
            return new ValidationError("body", "invalid JSON.", "Invalid request body.");

        if (!Enum.IsDefined(typeof(TravelcardTypeEnum), model.TravelcardType))
            return new ValidationError("travelcardType", "invalid enum value.", $"Invalid value '{model.TravelcardType}' for field 'travelcardType'.");

        if (model.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
            return new ValidationError("travelcardRequestedDate", "must be in the past.", "travelcardRequestedDate must be in the past.");

        if (model.TravelcardValidFrom > model.TravelcardValidTo)
            return new ValidationError("travelcardValidFrom", "must not be later than travelcardValidTo.", "travelcardValidFrom must not be later than travelcardValidTo.");

        if (model.TravelcardValidTo <= DateTimeOffset.UtcNow)
            return new ValidationError("travelcardValidTo", "must be in the future.", "travelcardValidTo must be in the future.");

        if (model.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
            return new ValidationError("travelcardValidFrom", "must be no later than one calendar month from today.", "travelcardValidFrom must be no later than one calendar month from today.");

        if (model.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && model.TravelcardUsableTo is null)
            return new ValidationError("travelcardUsableTo", "required for SixteenToSeventeen.", "travelcardUsableTo is required for SixteenToSeventeen travelcards.");

        if (model.TravelcardUsableTo is not null && model.TravelcardUsableTo <= DateTimeOffset.UtcNow)
            return new ValidationError("travelcardUsableTo", "must be in the future.", "travelcardUsableTo must be in the future.");

        if ((model.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen || model.TravelcardType == TravelcardTypeEnum.Veterans) && model.Cardholders.Any(c => c.CardholderType == CardholderTypeEnum.Secondary))
            return new ValidationError("cardholders", "secondary cardholder not allowed for this travelcard type.", "Secondary cardholder is not allowed for this travelcard type.");

        if (model.Cardholders.Count is < 1 or > 2)
            return new ValidationError("cardholders", "must contain one or two items.", "cardholders must contain one or two items.");

        if (model.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Primary) != 1)
            return new ValidationError("cardholders", "exactly one primary cardholder is required.", "Exactly one Primary cardholder is required.");

        if (model.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Secondary) > 1)
            return new ValidationError("cardholders", "at most one secondary cardholder is allowed.", "At most one Secondary cardholder is allowed.");

        foreach (var cardholder in model.Cardholders)
        {
            if (!Regex.IsMatch(cardholder.CardholderTitle, @"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-žºª .''’\-]+$"))
                return new ValidationError("cardholderTitle", "invalid format.", "cardholderTitle has invalid format.");
            if (!Regex.IsMatch(cardholder.CardholderForename, @"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\-]+$"))
                return new ValidationError("cardholderForename", "invalid format.", "cardholderForename has invalid format.");
            if (!Regex.IsMatch(cardholder.CardholderSurname, @"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\-]+$"))
                return new ValidationError("cardholderSurname", "invalid format.", "cardholderSurname has invalid format.");
            if (!Regex.IsMatch(cardholder.CardholderPhotoName, @"^(?!.*[×÷ˇ˘μ])[A-Za-z0-9À-ž _.\-()\[\]'',&+#]+$"))
                return new ValidationError("cardholderPhotoName", "invalid format.", "cardholderPhotoName has invalid format.");
            if (!(cardholder.CardholderPhotoRRSKey is not null || cardholder.CardholderPhotoURL is not null || cardholder.CardholderPhotoKey is not null))
                return new ValidationError("cardholderPhoto", "one of photo fields must be provided.", "One of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey must be provided.");
        }

        return null;
    }
}

public sealed record ValidationError(string Field, string Reason, string Message);