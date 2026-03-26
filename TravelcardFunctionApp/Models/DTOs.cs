using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelcardFunctionApp.Models
{
    public class TravelcardRequest
    {
        [JsonPropertyName("travelcardType")]
        public TravelcardType TravelcardType { get; set; } = TravelcardType.Young;

        [JsonPropertyName("travelcardValidFrom")]
        public DateTime TravelcardValidFrom { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("travelcardValidTo")]
        public DateTime TravelcardValidTo { get; set; } = DateTime.UtcNow.AddDays(30);

        [JsonPropertyName("travelcardName")]
        public string TravelcardName { get; set; } = string.Empty;

        [JsonPropertyName("travelcardNumber")]
        public string TravelcardNumber { get; set; } = string.Empty;

        [JsonPropertyName("travelcardRequestedDate")]
        public DateTime TravelcardRequestedDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("travelcardTransactionReference")]
        public string TravelcardTransactionReference { get; set; } = string.Empty;

        [JsonPropertyName("travelcardUsableTo")]
        public DateTime? TravelcardUsableTo { get; set; }

        [JsonPropertyName("cardholders")]
        public List<CardholderRequest> Cardholders { get; set; } = new List<CardholderRequest>();
    }

    public class CardholderRequest
    {
        [JsonPropertyName("cardholderTitle")]
        public string CardholderTitle { get; set; } = string.Empty;

        [JsonPropertyName("cardholderForename")]
        public string CardholderForename { get; set; } = string.Empty;

        [JsonPropertyName("cardholderSurname")]
        public string CardholderSurname { get; set; } = string.Empty;

        [JsonPropertyName("cardholderType")]
        public CardholderType CardholderType { get; set; } = CardholderType.Primary;

        [JsonPropertyName("cardholderPhotoName")]
        public string CardholderPhotoName { get; set; } = string.Empty;

        [JsonPropertyName("cardholderPhotoRRSKey")]
        public string CardholderPhotoRRSKey { get; set; } = string.Empty;

        [JsonPropertyName("cardholderPhotoURL")]
        public string CardholderPhotoURL { get; set; } = string.Empty;

        [JsonPropertyName("cardholderPhotoKey")]
        public string CardholderPhotoKey { get; set; } = string.Empty;
    }
}
