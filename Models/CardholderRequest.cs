using System.Text.Json.Serialization;

namespace TravelcardFunctionApp.Models;

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