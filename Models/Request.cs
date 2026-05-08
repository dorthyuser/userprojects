using System.Text.Json.Serialization;
using Travelcardlambdacsharp1105Lambda;

namespace Travelcardlambdacsharp1105Lambda.Models;

public sealed class CreateTravelcardRequest
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
    public List<CardholderRequest> Cardholders { get; set; } = new();
}

public sealed class CardholderRequest
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