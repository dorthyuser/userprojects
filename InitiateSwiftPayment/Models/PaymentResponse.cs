using System;
using System.Text.Json.Serialization;

namespace InitiateSwiftPayment.Models
{
    public class PaymentResponse
    {
        [JsonPropertyName("paymentId")]
        public string PaymentId { get; set; } = string.Empty;

        [JsonPropertyName("swiftMsgRef")]
        public string SwiftMsgRef { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("submittedAt")]
        public string SubmittedAt { get; set; } = string.Empty;
    }
}
