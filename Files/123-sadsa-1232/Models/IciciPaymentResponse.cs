namespace _123_sadsa_1232.Models;

public sealed record IciciPaymentResponse(
    string TransactionId,
    string Status,
    string CorrelationId,
    string? Message
);