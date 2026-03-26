using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace travelcardservice.Models
{
    public class TravelcardRequest
    {
        [JsonPropertyName("travelcardType")]
        public TravelcardType TravelcardType { get; set; }

        [JsonPropertyName("travelcardValidFrom")]
        public DateTime TravelcardValidFrom { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("travelcardValidTo")]
        public DateTime TravelcardValidTo { get; set; } = DateTime.UtcNow.AddDays(1);

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
}
