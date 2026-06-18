using csharpapi248pm.Models;

namespace csharpapi248pm.Services;

public interface IAdverseEventService
{
    Task<AdverseEventSubmitResponse> SubmitAsync(AdverseEventRequest request, CancellationToken cancellationToken);
    Task<AdverseEventNotificationsResponse> GetNotificationsAsync(AdverseEventNotificationsQueryRequest request, CancellationToken cancellationToken);
}