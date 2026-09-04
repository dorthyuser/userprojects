using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Paymentcsharp441Lambda.Models;

public sealed class InitiatePaymentRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(30)]
    [JsonPropertyName("planId")]
    public string? PlanId { get; set; }

    [Required]
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(30)]
    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [MaxLength(500)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

public sealed class VerifyPaymentRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(25)]
    [JsonPropertyName("paymentId")]
    public string? PaymentId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    [JsonPropertyName("gatewayPaymentId")]
    public string? GatewayPaymentId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    [JsonPropertyName("gatewayOrderId")]
    public string? GatewayOrderId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    [JsonPropertyName("gatewaySignature")]
    public string? GatewaySignature { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(20)]
    [JsonPropertyName("verificationSource")]
    public string? VerificationSource { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(20)]
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}