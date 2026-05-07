using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Travelcardchsarplambda1050Lambda.Models;

public sealed class CreateTravelcardRequest
{
    [JsonPropertyName("travelcardType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TravelcardTypeEnum TravelcardType { get; set; }

    [JsonPropertyName("travelcardValidFrom")]
    public DateTimeOffset TravelcardValidFrom { get; set; }

    [JsonPropertyName("travelcardValidTo")]
    public DateTimeOffset TravelcardValidTo { get; set; }

    [JsonPropertyName("travelcardName")]
    [StringLength(255)]
    public string? TravelcardName { get; set; }

    [JsonPropertyName("travelcardNumber")]
    [StringLength(22, MinimumLength = 11)]
    public string TravelcardNumber { get; set; } = null!;

    [JsonPropertyName("travelcardRequestedDate")]
    public DateTimeOffset TravelcardRequestedDate { get; set; }

    [JsonPropertyName("travelcardTransactionReference")]
    [StringLength(15, MinimumLength = 15)]
    public string TravelcardTransactionReference { get; set; } = null!;

    [JsonPropertyName("travelcardUsableTo")]
    public DateTimeOffset? TravelcardUsableTo { get; set; }

    [JsonPropertyName("cardholders")]
    [MinLength(1)]
    [MaxLength(2)]
    public List<CardholderRequest> Cardholders { get; set; } = new();
}

public sealed class CardholderRequest
{
    [JsonPropertyName("cardholderTitle")]
    [StringLength(15, MinimumLength = 1)]
    public string CardholderTitle { get; set; } = null!;

    [JsonPropertyName("cardholderForename")]
    [StringLength(100, MinimumLength = 1)]
    public string CardholderForename { get; set; } = null!;

    [JsonPropertyName("cardholderSurname")]
    [StringLength(100, MinimumLength = 1)]
    public string CardholderSurname { get; set; } = null!;

    [JsonPropertyName("cardholderType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CardholderTypeEnum CardholderType { get; set; }

    [JsonPropertyName("cardholderPhotoName")]
    [StringLength(100, MinimumLength = 1)]
    public string CardholderPhotoName { get; set; } = null!;

    [JsonPropertyName("cardholderPhotoRRSKey")]
    [StringLength(42)]
    public string? CardholderPhotoRRSKey { get; set; }

    [JsonPropertyName("cardholderPhotoURL")]
    [StringLength(2048)]
    public string? CardholderPhotoURL { get; set; }

    [JsonPropertyName("cardholderPhotoKey")]
    [StringLength(42)]
    public string? CardholderPhotoKey { get; set; }
}