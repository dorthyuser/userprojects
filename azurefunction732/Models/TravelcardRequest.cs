using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelcardService.Models
{
    public class TravelcardRequest
    {
        [JsonPropertyName("travelcardType")]
        public TravelcardType TravelcardType { get; set; } = TravelcardType.Young;

        [JsonPropertyName("travelcardValidFrom")]
        public DateTime TravelcardValidFrom { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("travelcardValidTo")]
        public DateTime TravelcardValidTo { get; set; } = DateTime.UtcNow.AddMonths(1);

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
        public List<CardholderDto> Cardholders { get; set; } = new List<CardholderDto>();
    }

    public class CardholderDto
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
        public string CardholderPhotoRrsKey { get; set; } = string.Empty;

        [JsonPropertyName("cardholderPhotoURL")]
        public string CardholderPhotoUrl { get; set; } = string.Empty;

        [JsonPropertyName("cardholderPhotoKey")]
        public string CardholderPhotoKey { get; set; } = string.Empty;
    }
}
