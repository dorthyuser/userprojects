using System;

namespace InitiateSwiftPayment.Models
{
    public class PaymentResponse
    {
        public string PaymentId { get; set; } = string.Empty;
        public string SwiftMsgRef { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

    public class PaymentRecord
    {
        public int Id { get; set; }
        public BankSwiftPaymentTypeEnum PaymentType { get; set; } = BankSwiftPaymentTypeEnum.MT103;
        public decimal PaymentAmount { get; set; } = 0.00M;
        public string PaymentCurrency { get; set; } = string.Empty;
        public string DebtorAccountIBAN { get; set; } = string.Empty;
        public string DebtorBIC { get; set; } = string.Empty;
        public string CreditorAccountIBAN { get; set; } = string.Empty;
        public string CreditorBIC { get; set; } = string.Empty;
        public string CreditorName { get; set; } = string.Empty;
        public string? RemittanceInfo { get; set; }
        public DateTime RequestedExecutionDate { get; set; } = DateTime.UtcNow.Date;
        public string EndToEndId { get; set; } = string.Empty;
        public string? SwiftMsgRef { get; set; }
        public BankPaymentStatusEnum Status { get; set; } = BankPaymentStatusEnum.PENDING;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public Guid IdempotencyKey { get; set; } = Guid.Empty;
    }
}
