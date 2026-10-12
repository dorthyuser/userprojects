namespace AdverseEventNotificationService.Api.Contracts;

public sealed record AdverseEventResponseDto(
    string Status,
    string AeId,
    string NotificationId,
    bool SnsPublished,
    string? SnsMessageId,
    DateTimeOffset ReceivedAt);