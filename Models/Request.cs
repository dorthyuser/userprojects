using System.Text.Json.Serialization;
using Travelcardlambdacsharp847Lambda.Services;

namespace Travelcardlambdacsharp847Lambda.Models;

public sealed class Request
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
    public string TravelcardNumber { get; set; } = null!;

    [JsonPropertyName("travelcardRequestedDate")]
    public DateTimeOffset TravelcardRequestedDate { get; set; }

    [JsonPropertyName("travelcardTransactionReference")]
    public string TravelcardTransactionReference { get; set; } = null!;

    [JsonPropertyName("travelcardUsableTo")]
    public DateTimeOffset? TravelcardUsableTo { get; set; }

    [JsonPropertyName("cardholders")]
    public List<Cardholder> Cardholders { get; set; } = new();
}

public sealed class Cardholder
{
    [JsonPropertyName("cardholderTitle")]
    public string CardholderTitle { get; set; } = null!;

    [JsonPropertyName("cardholderForename")]
    public string CardholderForename { get; set; } = null!;

    [JsonPropertyName("cardholderSurname")]
    public string CardholderSurname { get; set; } = null!;

    [JsonPropertyName("cardholderType")]
    public CardholderTypeEnum CardholderType { get; set; }

    [JsonPropertyName("cardholderPhotoName")]
    public string CardholderPhotoName { get; set; } = null!;

    [JsonPropertyName("cardholderPhotoRRSKey")]
    public string? CardholderPhotoRRSKey { get; set; }

    [JsonPropertyName("cardholderPhotoURL")]
    public string? CardholderPhotoURL { get; set; }

    [JsonPropertyName("cardholderPhotoKey")]
    public string? CardholderPhotoKey { get; set; }
}