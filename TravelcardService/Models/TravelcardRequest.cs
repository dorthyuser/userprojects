using System.Text.Json.Serialization;
using System;

namespace TravelcardService.Models
{
    public class TravelcardRequest
    {
        [JsonPropertyName("cardNumber")]
        public string? CardNumber { get; set; }

        [JsonPropertyName("holderName")]
        public string? HolderName { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; } = 0m;

        [JsonPropertyName("type")]
        public TravelcardType Type { get; set; } = TravelcardType.Standard;

        [JsonPropertyName("purchaseDateUtc")]
        public DateTime? PurchaseDateUtc { get; set; }
    }
}
