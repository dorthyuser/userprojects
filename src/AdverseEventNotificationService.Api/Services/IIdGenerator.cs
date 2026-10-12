namespace AdverseEventNotificationService.Api.Services;

public interface IIdGenerator
{
    Task<string> GenerateAeIdAsync(CancellationToken cancellationToken);
    Task<string> GenerateNotificationIdAsync(CancellationToken cancellationToken);
}