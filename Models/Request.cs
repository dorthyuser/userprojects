using System.Text.Json.Serialization;

namespace Travelcardlambda1010Lambda.Models;

public sealed class TravelcardRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TravelcardType TravelcardType { get; set; }

    public DateTimeOffset TravelcardValidFrom { get; set; }
    public DateTimeOffset TravelcardValidTo { get; set; }
    public string? TravelcardName { get; set; }
    public string TravelcardNumber { get; set; } = null!;
    public DateTimeOffset TravelcardRequestedDate { get; set; }
    public string TravelcardTransactionReference { get; set; } = null!;
    public DateTimeOffset? TravelcardUsableTo { get; set; }
    public List<CardholderRequest> Cardholders { get; set; } = new();
}

public sealed class CardholderRequest
{
    public string CardholderTitle { get; set; } = null!;
    public string CardholderForename { get; set; } = null!;
    public string CardholderSurname { get; set; } = null!;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CardholderType CardholderType { get; set; }

    public string CardholderPhotoName { get; set; } = null!;
    public string? CardholderPhotoRRSKey { get; set; }
    public string? CardholderPhotoURL { get; set; }
    public string? CardholderPhotoKey { get; set; }
}

public sealed class ErrorResponse
{
    public ErrorDetail Error { get; set; } = new();
}

public sealed class ErrorDetail
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}