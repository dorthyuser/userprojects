using System.Data.Common;
using Microsoft.Extensions.Options;
using Npgsql;
using PrimeNumberService.Configuration;
using PrimeNumberService.Security;

namespace PrimeNumberService.Data;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly ISecretProvider _secretProvider;
    private readonly ServiceOptions _options;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;
    private NpgsqlDataSource? _dataSource;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public NpgsqlConnectionFactory(ISecretProvider secretProvider, IOptions<ServiceOptions> options, ILogger<NpgsqlConnectionFactory> logger)
    {
        _secretProvider = secretProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_dataSource is null)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (_dataSource is null)
                {
                    var secret = await _secretProvider.GetDatabaseSecretAsync(_options.SecretsManagerSecretName, cancellationToken);
                    var builder = new NpgsqlDataSourceBuilder(BuildConnectionString(secret));
                    _dataSource = builder.Build();
                    _logger.LogInformation("Database pool initialized");
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return connection;
    }

    private string BuildConnectionString(DatabaseSecret secret)
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = secret.Host,
            Port = secret.Port,
            Database = secret.DbName,
            Username = secret.Username,
            Password = secret.Password,
            SearchPath = _options.Database.SearchPath,
            Timeout = _options.Database.ConnectTimeoutSeconds,
            CommandTimeout = _options.Database.CommandTimeoutSeconds,
            Pooling = true,
            MinPoolSize = _options.Database.MinPoolSize,
            MaxPoolSize = _options.Database.MaxPoolSize,
            IncludeErrorDetail = false
        };
        return csb.ConnectionString;
    }
}