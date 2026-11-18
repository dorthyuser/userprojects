using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BusTravelAccountsSaFunction;

public sealed class HealthRepository(NpgsqlDataSource dataSource, ILogger<HealthRepository> logger) : IHealthRepository
{
    public async Task CheckDependenciesAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));
        logger.LogInformation("SELECT 1 readiness check outcome: success");
    }
}
