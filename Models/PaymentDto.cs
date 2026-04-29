namespace 123-sadsa-1232.Models;

public sealed record PaymentDto(
    long Id,
    long UserId,
    long? OrderId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string? Provider,
    string? ProviderTransactionId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PaidAt,
    string? Description,
    string? Metadata
);