using System.Text.Json;

namespace _123_sadsa_1232.Models;

public sealed class Payment
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string Status { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string? Description { get; set; }
    public JsonDocument? Metadata { get; set; }
}