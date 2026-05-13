using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace httptestingapi.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TravelcardTypeEnum
{
    Young,
    Barcklays,
    DevonandCornwall,
    TwoTogether,
    Family,
    Senior,
    DisabledPersons,
    Network,
    TwentySixToThirty,
    SixteenToSeventeen,
    Veterans
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardholderTypeEnum
{
    Primary,
    Secondary
}

public sealed class CreateTravelcardRequest
{
    [Required]
    public TravelcardTypeEnum TravelcardType { get; set; }

    [Required]
    public DateTimeOffset TravelcardValidFrom { get; set; }

    [Required]
    public DateTimeOffset TravelcardValidTo { get; set; }

    [MaxLength(255)]
    public string? TravelcardName { get; set; }

    [Required]
    [MinLength(11)]
    [MaxLength(22)]
    public string TravelcardNumber { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset TravelcardRequestedDate { get; set; }

    [Required]
    [StringLength(15, MinimumLength = 15)]
    public string TravelcardTransactionReference { get; set; } = string.Empty;

    public DateTimeOffset? TravelcardUsableTo { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(2)]
    public List<CardholderRequest> Cardholders { get; set; } = new();
}

public sealed class CardholderRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(15)]
    public string CardholderTitle { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string CardholderForename { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string CardholderSurname { get; set; } = string.Empty;

    [Required]
    public CardholderTypeEnum CardholderType { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string CardholderPhotoName { get; set; } = string.Empty;

    [MinLength(39)]
    [MaxLength(42)]
    public string? CardholderPhotoRRSKey { get; set; }

    [MinLength(20)]
    [MaxLength(2048)]
    public string? CardholderPhotoURL { get; set; }

    [MinLength(39)]
    [MaxLength(42)]
    public string? CardholderPhotoKey { get; set; }
}

public sealed class CreateTravelcardResponse
{
    [JsonPropertyName("travelcardId")]
    public Guid TravelcardId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
