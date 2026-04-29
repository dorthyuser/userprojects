namespace 123-sadsa-1232.Models;

public sealed record IciciPaymentResponse(
    string TransactionId,
    string Status,
    string CorrelationId,
    string? Message
);