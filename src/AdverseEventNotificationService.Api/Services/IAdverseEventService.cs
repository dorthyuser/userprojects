using AdverseEventNotificationService.Api.Contracts;

namespace AdverseEventNotificationService.Api.Services;

public interface IAdverseEventService
{
    Task<AdverseEventResponseDto> SubmitAsync(AdverseEventRequestDto request, CancellationToken cancellationToken);
    Task<AdverseEventNotificationResponseDto> GetNotificationsAsync(NotificationQueryDto query, CancellationToken cancellationToken);
}