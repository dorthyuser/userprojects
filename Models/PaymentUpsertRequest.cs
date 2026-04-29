namespace 123-sadsa-1232.Models;

public sealed record PaymentUpsertRequest(
    long UserId,
    long? OrderId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string? Provider,
    string? ProviderTransactionId,
    string Status,
    DateTimeOffset? PaidAt,
    string? Description,
    string? Metadata
);