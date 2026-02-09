using System;
using System.Text.Json.Serialization;

namespace TravelCardFunctionApp.Models
{
    public class TravelCard
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("cardNumber")]
        public string CardNumber { get; set; } = string.Empty;

        [JsonPropertyName("holderName")]
        public string HolderName { get; set; } = string.Empty;

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateTime ExpiryDate { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
