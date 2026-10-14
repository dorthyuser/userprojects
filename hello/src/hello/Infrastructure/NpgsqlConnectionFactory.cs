using Npgsql;

namespace hello.Infrastructure;

public interface IDbConnectionFactory
{
    Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly ISecretProvider _secretProvider;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;
    private NpgsqlDataSource? _dataSource;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public NpgsqlConnectionFactory(ISecretProvider secretProvider, ILogger<NpgsqlConnectionFactory> logger)
    {
        _secretProvider = secretProvider;
        _logger = logger;
    }

    public async Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_dataSource is null)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (_dataSource is null)
                {
                    var secret = await _secretProvider.GetDatabaseSecretAsync(cancellationToken);
                    var builder = new NpgsqlDataSourceBuilder(BuildConnectionString(secret));
                    _dataSource = builder.Build();
                    _logger.LogInformation("psycopg2 pool created from Secrets Manager: pool=primenumber2Pool");
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        var connection = await _dataSource!.OpenConnectionAsync(cancellationToken);
        return connection;
    }

    private static string BuildConnectionString(DatabaseSecret secret)
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = secret.Host,
            Port = secret.Port,
            Database = secret.DbName,
            Username = secret.Username,
            Password = secret.Password,
            SearchPath = "public",
            Timeout = 5,
            Pooling = true,
            MaximumPoolSize = 20,
            MinimumPoolSize = 2,
            IncludeErrorDetail = false
        };
        return csb.ConnectionString;
    }
}
