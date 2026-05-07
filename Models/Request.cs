using System.Text.Json.Serialization;

namespace Travelcardcharplambda1031Lambda.Models;

public sealed class CreateTravelcardRequest
{
    [JsonPropertyName("travelcardType")]
    public TravelcardType TravelcardType { get; init; }

    [JsonPropertyName("travelcardValidFrom")]
    public DateTimeOffset TravelcardValidFrom { get; init; }

    [JsonPropertyName("travelcardValidTo")]
    public DateTimeOffset TravelcardValidTo { get; init; }

    [JsonPropertyName("travelcardName")]
    public string? TravelcardName { get; init; }

    [JsonPropertyName("travelcardNumber")]
    public string TravelcardNumber { get; init; } = string.Empty;

    [JsonPropertyName("travelcardRequestedDate")]
    public DateTimeOffset TravelcardRequestedDate { get; init; }

    [JsonPropertyName("travelcardTransactionReference")]
    public string TravelcardTransactionReference { get; init; } = string.Empty;

    [JsonPropertyName("travelcardUsableTo")]
    public DateTimeOffset? TravelcardUsableTo { get; init; }

    [JsonPropertyName("cardholders")]
    public List<CardholderRequest> Cardholders { get; init; } = new();
}

public sealed class CardholderRequest
{
    [JsonPropertyName("cardholderTitle")]
    public string CardholderTitle { get; init; } = string.Empty;

    [JsonPropertyName("cardholderForename")]
    public string CardholderForename { get; init; } = string.Empty;

    [JsonPropertyName("cardholderSurname")]
    public string CardholderSurname { get; init; } = string.Empty;

    [JsonPropertyName("cardholderType")]
    public CardholderType CardholderType { get; init; }

    [JsonPropertyName("cardholderPhotoName")]
    public string CardholderPhotoName { get; init; } = string.Empty;

    [JsonPropertyName("cardholderPhotoRRSKey")]
    public string? CardholderPhotoRRSKey { get; init; }

    [JsonPropertyName("cardholderPhotoURL")]
    public string? CardholderPhotoURL { get; init; }

    [JsonPropertyName("cardholderPhotoKey")]
    public string? CardholderPhotoKey { get; init; }
}