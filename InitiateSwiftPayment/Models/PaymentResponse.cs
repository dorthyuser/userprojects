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
        public DateTime SubmittedAt { get; set; }
    }

    public class ErrorResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public string? Details { get; set; }
    }

    public class ValidationErrorResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("errors")]
        public System.Collections.Generic.List<string> Errors { get; set; } = new System.Collections.Generic.List<string>();
    }
}
