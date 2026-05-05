using System.Text.Json.Serialization;

namespace TravelcardchsarplambdaLambda.Models;

public class Request
{
    [JsonPropertyName("travelcardType")]
    public TravelcardTypeEnum TravelcardType { get; set; }

    [JsonPropertyName("travelcardValidFrom")]
    public DateTimeOffset TravelcardValidFrom { get; set; }

    [JsonPropertyName("travelcardValidTo")]
    public DateTimeOffset TravelcardValidTo { get; set; }

    [JsonPropertyName("travelcardName")]
    public string? TravelcardName { get; set; }

    [JsonPropertyName("travelcardNumber")]
    public string TravelcardNumber { get; set; } = string.Empty;

    [JsonPropertyName("travelcardRequestedDate")]
    public DateTimeOffset TravelcardRequestedDate { get; set; }

    [JsonPropertyName("travelcardTransactionReference")]
    public string TravelcardTransactionReference { get; set; } = string.Empty;

    [JsonPropertyName("travelcardUsableTo")]
    public DateTimeOffset? TravelcardUsableTo { get; set; }

    [JsonPropertyName("cardholders")]
    public List<Cardholder> Cardholders { get; set; } = new();
}

public class Cardholder
{
    [JsonPropertyName("cardholderTitle")]
    public string CardholderTitle { get; set; } = string.Empty;

    [JsonPropertyName("cardholderForename")]
    public string CardholderForename { get; set; } = string.Empty;

    [JsonPropertyName("cardholderSurname")]
    public string CardholderSurname { get; set; } = string.Empty;

    [JsonPropertyName("cardholderType")]
    public CardholderTypeEnum CardholderType { get; set; }

    [JsonPropertyName("cardholderPhotoName")]
    public string CardholderPhotoName { get; set; } = string.Empty;

    [JsonPropertyName("cardholderPhotoRRSKey")]
    public string? CardholderPhotoRRSKey { get; set; }

    [JsonPropertyName("cardholderPhotoURL")]
    public string? CardholderPhotoURL { get; set; }

    [JsonPropertyName("cardholderPhotoKey")]
    public string? CardholderPhotoKey { get; set; }
}

public class CreateResult
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public ErrorDetail Error { get; set; } = new();
}

public class ErrorDetail
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}