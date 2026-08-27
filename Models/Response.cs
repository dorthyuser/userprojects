using System.Text.Json.Serialization;
namespace Paymentcsharp441Lambda.Models;
public sealed class ErrorResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
public sealed class InitiatePaymentResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("paymentId")]
    public string PaymentId { get; set; } = string.Empty;
    [JsonPropertyName("gatewayOrderId")]
    public string GatewayOrderId { get; set; } = string.Empty;
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("initiatedAt")]
    public DateTime InitiatedAt { get; set; }
}
public sealed class VerifyPaymentResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("verificationId")]
    public string VerificationId { get; set; } = string.Empty;
    [JsonPropertyName("paymentId")]
    public string PaymentId { get; set; } = string.Empty;
    [JsonPropertyName("invoiceId")]
    public string InvoiceId { get; set; } = string.Empty;
    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = string.Empty;
    [JsonPropertyName("subscriptionActivated")]
    public bool SubscriptionActivated { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("verifiedAt")]
    public DateTime VerifiedAt { get; set; }
}
public sealed class PaymentRecordResponse
{
    [JsonPropertyName("paymentId")]
    public string PaymentId { get; set; } = string.Empty;
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    [JsonPropertyName("planId")]
    public string PlanId { get; set; } = string.Empty;
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("gatewayOrderId")]
    public string GatewayOrderId { get; set; } = string.Empty;
    [JsonPropertyName("gatewayPaymentId")]
    public string? GatewayPaymentId { get; set; }
    [JsonPropertyName("invoiceId")]
    public string? InvoiceId { get; set; }
    [JsonPropertyName("initiatedAt")]
    public DateTime InitiatedAt { get; set; }
    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }
}
public sealed class GetPaymentsResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("total")]
    public long Total { get; set; }
    [JsonPropertyName("page")]
    public int Page { get; set; }
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
    [JsonPropertyName("payments")]
    public List<PaymentRecordResponse> Payments { get; set; } = new();
}