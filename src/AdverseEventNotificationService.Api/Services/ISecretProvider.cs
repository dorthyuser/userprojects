using AdverseEventNotificationService.Api.Configuration;

namespace AdverseEventNotificationService.Api.Services;

public interface ISecretProvider
{
    Task<DatabaseConnectionSettings?> GetDatabaseConnectionSettingsAsync(CancellationToken cancellationToken);
}