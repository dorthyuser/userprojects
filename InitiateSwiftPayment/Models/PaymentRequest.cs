using System;
using System.Text.Json.Serialization;

namespace InitiateSwiftPayment.Models
{
    public class PaymentRequest
    {
        [JsonPropertyName("paymentType")]
        public BankSwiftPaymentTypeEnum PaymentType { get; set; } = BankSwiftPaymentTypeEnum.MT103;

        [JsonPropertyName("paymentAmount")]
        public decimal PaymentAmount { get; set; }

        [JsonPropertyName("paymentCurrency")]
        public string PaymentCurrency { get; set; } = string.Empty;

        [JsonPropertyName("debtorAccountIBAN")]
        public string DebtorAccountIBAN { get; set; } = string.Empty;

        [JsonPropertyName("debtorBIC")]
        public string DebtorBIC { get; set; } = string.Empty;

        [JsonPropertyName("creditorAccountIBAN")]
        public string CreditorAccountIBAN { get; set; } = string.Empty;

        [JsonPropertyName("creditorBIC")]
        public string CreditorBIC { get; set; } = string.Empty;

        [JsonPropertyName("creditorName")]
        public string CreditorName { get; set; } = string.Empty;

        [JsonPropertyName("remittanceInfo")]
        public string? RemittanceInfo { get; set; }

        [JsonPropertyName("requestedExecutionDate")]
        public DateTime RequestedExecutionDate { get; set; }

        [JsonPropertyName("endToEndId")]
        public string EndToEndId { get; set; } = string.Empty;
    }
}
