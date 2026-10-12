namespace AdverseEventNotificationService.Api.Contracts;

public sealed record AdverseEventNotificationResponseDto(
    string Status,
    long Total,
    int Page,
    int PageSize,
    IReadOnlyList<AdverseEventNotificationItemDto> Notifications);