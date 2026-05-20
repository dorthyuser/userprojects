using System.Text.Json.Serialization;

namespace DemotravelcardLambda.Models;

public sealed class Request
{
    [JsonPropertyName("travelcardType")]
    public TravelcardType? TravelcardType { get; set; }

    [JsonPropertyName("travelcardValidFrom")]
    public DateTimeOffset TravelcardValidFrom { get; set; }

    [JsonPropertyName("travelcardValidTo")]
    public DateTimeOffset TravelcardValidTo { get; set; }

    [JsonPropertyName("travelcardName")]
    public string? TravelcardName { get; set; }

    [JsonPropertyName("travelcardNumber")]
    public string? TravelcardNumber { get; set; }

    [JsonPropertyName("travelcardRequestedDate")]
    public DateTimeOffset TravelcardRequestedDate { get; set; }

    [JsonPropertyName("travelcardTransactionReference")]
    public string? TravelcardTransactionReference { get; set; }

    [JsonPropertyName("travelcardUsableTo")]
    public DateTimeOffset? TravelcardUsableTo { get; set; }

    [JsonPropertyName("cardholders")]
    public List<CardholderRequest> Cardholders { get; set; } = new();
}

public sealed class CardholderRequest
{
    [JsonPropertyName("cardholderTitle")]
    public string? CardholderTitle { get; set; }

    [JsonPropertyName("cardholderForename")]
    public string? CardholderForename { get; set; }

    [JsonPropertyName("cardholderSurname")]
    public string? CardholderSurname { get; set; }

    [JsonPropertyName("cardholderType")]
    public CardholderType? CardholderType { get; set; }

    [JsonPropertyName("cardholderPhotoName")]
    public string? CardholderPhotoName { get; set; }

    [JsonPropertyName("cardholderPhotoRRSKey")]
    public string? CardholderPhotoRRSKey { get; set; }

    [JsonPropertyName("cardholderPhotoURL")]
    public string? CardholderPhotoURL { get; set; }

    [JsonPropertyName("cardholderPhotoKey")]
    public string? CardholderPhotoKey { get; set; }
}