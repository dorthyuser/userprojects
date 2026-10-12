using Dapper;

namespace AdverseEventNotificationService.Api.Services;

public sealed class SequentialIdGenerator : IIdGenerator
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SequentialIdGenerator(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<string> GenerateAeIdAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var value = await connection.ExecuteScalarAsync<long>(new CommandDefinition("SELECT nextval('ae_id_seq')", cancellationToken: cancellationToken));
        return $"AE-{DateTimeOffset.UtcNow:yyyy}-{value:D6}";
    }

    public async Task<string> GenerateNotificationIdAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var value = await connection.ExecuteScalarAsync<long>(new CommandDefinition("SELECT nextval('notif_id_seq')", cancellationToken: cancellationToken));
        return $"NOTIF-{DateTimeOffset.UtcNow:yyyy}-{value:D6}";
    }
}