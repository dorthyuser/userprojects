using csharpapi248pm.Models;

namespace csharpapi248pm.Repositories;

public interface IAdverseEventRepository
{
    Task<AdverseEventSubmitResponse> SubmitAsync(AdverseEventRequest request, CancellationToken cancellationToken);
    Task<AdverseEventNotificationsResponse> GetNotificationsAsync(AdverseEventNotificationsQueryRequest request, CancellationToken cancellationToken);
}