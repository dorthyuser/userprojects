using System;
using System.Text.Json.Serialization;

namespace InitiateSwiftPayment.Models
{
    public class InitiateSwiftPaymentResponse
    {
        [JsonPropertyName("paymentId")]
        public string PaymentId { get; set; } = string.Empty;

        [JsonPropertyName("swiftMsgRef")]
        public string SwiftMsgRef { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public BankPaymentStatus Status { get; set; } = BankPaymentStatus.ACCEPTED;

        [JsonPropertyName("submittedAt")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
