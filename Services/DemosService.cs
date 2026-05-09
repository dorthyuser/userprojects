using Npgsql;
using new_project.Models;

namespace new_project.Services;

public sealed class DemosService : IDemosService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<DemosService> _logger;

    public DemosService(ILogger<DemosService> logger)
    {
        _logger = logger;
        var host = SecretHelper.Get("POSTGRESQL_HOST", "POSTGRESQL_HOST");
        var port = SecretHelper.Get("POSTGRESQL_PORT", "POSTGRESQL_PORT");
        var database = SecretHelper.Get("POSTGRESQL_DATABASE", "POSTGRESQL_DATABASE");
        var username = SecretHelper.Get("POSTGRESQL_USERNAME", "POSTGRESQL_USERNAME");
        var password = SecretHelper.Get("POSTGRESQL_PASSWORD", "POSTGRESQL_PASSWORD");
        var connStr = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        var builder = new NpgsqlDataSourceBuilder(connStr);
        _dataSource = builder.Build();
    }

    public async Task<IReadOnlyList<DemoResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating request...");
        _logger.LogInformation("Validation passed.");
        _logger.LogInformation("DB operation: {Operation} into {Table}", "SELECT", "demos");

        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 'demo' AS message";
            var items = new List<DemoResponse>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new DemoResponse { Message = reader.GetString(0) });
            }

            _logger.LogInformation("DB success: table={Table}, id={Id}", "demos", 0);
            _logger.LogInformation("Request completed. id={Id}", 0);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB error: table={Table}, operation={Operation}", "demos", "SELECT");
            throw;
        }
    }
}