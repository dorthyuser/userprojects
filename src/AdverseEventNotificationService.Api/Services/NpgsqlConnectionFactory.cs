using System.Data.Common;
using AdverseEventNotificationService.Api.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AdverseEventNotificationService.Api.Services;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly ISecretProvider _secretProvider;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;

    public NpgsqlConnectionFactory(IConfiguration configuration, ISecretProvider secretProvider, ILogger<NpgsqlConnectionFactory> logger)
    {
        _configuration = configuration;
        _secretProvider = secretProvider;
        _logger = logger;
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var settings = await _secretProvider.GetDatabaseConnectionSettingsAsync(cancellationToken) ?? ReadFromEnvironment();
        if (settings is null)
        {
            throw new InvalidOperationException("Missing required database configuration");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = settings.Host,
            Port = settings.Port,
            Database = settings.Database,
            Username = settings.Username,
            Password = settings.Password,
            Pooling = true,
            MaximumPoolSize = 10,
            MinimumPoolSize = 1,
            Timeout = 30,
            CommandTimeout = 30
        };

        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private DatabaseConnectionSettings? ReadFromEnvironment()
    {
        var host = _configuration[SecretNames.PostgresHost] ?? Environment.GetEnvironmentVariable(SecretNames.PostgresHost);
        var portText = _configuration[SecretNames.PostgresPort] ?? Environment.GetEnvironmentVariable(SecretNames.PostgresPort);
        var database = _configuration[SecretNames.PostgresDatabase] ?? Environment.GetEnvironmentVariable(SecretNames.PostgresDatabase);
        var username = _configuration[SecretNames.PostgresUsername] ?? Environment.GetEnvironmentVariable(SecretNames.PostgresUsername);
        var password = _configuration[SecretNames.PostgresPassword] ?? Environment.GetEnvironmentVariable(SecretNames.PostgresPassword);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portText) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        return new DatabaseConnectionSettings(host, int.Parse(portText), database, username, password);
    }
}