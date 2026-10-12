using System.Data.Common;

namespace AdverseEventNotificationService.Api.Services;

public interface IDbConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}