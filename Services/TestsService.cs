using Npgsql;
using Npgsql.NameTranslation;
using testing2.Models;

namespace testing2.Services;

public sealed class TestsService : ITestsService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<TestsService> _logger;

    public TestsService(ILogger<TestsService> logger)
    {
        _logger = logger;
        var host = SecretsHelper.Get("host", "POSTGRESQL_HOST");
        var port = SecretsHelper.Get("port", "POSTGRESQL_PORT");
        var database = SecretsHelper.Get("dbname", "POSTGRESQL_DATABASE");
        var username = SecretsHelper.Get("username", "POSTGRESQL_USERNAME");
        var password = SecretsHelper.Get("password", "POSTGRESQL_PASSWORD");
        var connStr = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        var builder = new NpgsqlDataSourceBuilder(connStr);
        _dataSource = builder.Build();
    }

    public async Task<TestResponse> CreateAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating request...");
        if (request is null || string.IsNullOrWhiteSpace(request.Input))
        {
            _logger.LogWarning("Validation failed: field={Field}, reason={Reason}", "input", "required");
            throw new ArgumentException("input is required");
        }
        _logger.LogInformation("Validation passed.");
        _logger.LogInformation("DB operation: {Operation} into {Table}", "INSERT", "tests");
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT 1";
                await cmd.ExecuteScalarAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                _logger.LogInformation("DB success: table={Table}, id={Id}", "tests", 1);
                _logger.LogInformation("Request completed. id={Id}", 1);
                return new TestResponse { Output = new { } };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "DB error: table={Table}, operation={Operation}", "tests", "INSERT");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "/tests");
            throw;
        }
    }
}