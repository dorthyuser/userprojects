using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelCardFunctionApp.Models;

public class CreateTravelcardRequest
{
    [JsonPropertyName("travelcardType")]
    public travelcard_type_enum TravelcardType { get; set; }

    [JsonPropertyName("travelcardValidFrom")]
    public DateTime TravelcardValidFrom { get; set; }

    [JsonPropertyName("travelcardValidTo")]
    public DateTime TravelcardValidTo { get; set; }

    [JsonPropertyName("travelcardName")]
    public string? TravelcardName { get; set; }

    [JsonPropertyName("travelcardNumber")]
    public string? TravelcardNumber { get; set; }

    [JsonPropertyName("travelcardRequestedDate")]
    public DateTime TravelcardRequestedDate { get; set; }

    [JsonPropertyName("travelcardTransactionReference")]
    public string? TravelcardTransactionReference { get; set; }

    [JsonPropertyName("travelcardUsableTo")]
    public DateTime? TravelcardUsableTo { get; set; }

    [JsonPropertyName("cardholders")]
    public List<CardholderRequest> Cardholders { get; set; } = new();
}
