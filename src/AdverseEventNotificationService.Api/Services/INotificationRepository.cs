using AdverseEventNotificationService.Api.Contracts;
using AdverseEventNotificationService.Api.Domain;

namespace AdverseEventNotificationService.Api.Services;

public interface INotificationRepository
{
    Task InsertAsync(AdverseEventNotificationEntity entity, CancellationToken cancellationToken);
    Task<(long Total, IReadOnlyList<AdverseEventNotificationItemDto> Items)> SearchAsync(NotificationQueryDto query, int page, int pageSize, CancellationToken cancellationToken);
}