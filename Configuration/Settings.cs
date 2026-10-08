using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Npgsql;

namespace BusTravelAccountsSaMainLambda;

public sealed class Settings
{
    private static readonly Lazy<Settings> LazyInstance = new(() => new Settings());
    private readonly string? _awsSecretName;
    private readonly string? _dbHost;
    private readonly string? _dbPort;
    private readonly string? _dbName;
    private readonly string? _dbUsername;
    private readonly string? _dbPassword;
    private readonly object _lock = new();
    private string? _cachedConnectionString;

    private Settings()
    {
        HttpsListenerPath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH") ?? "api/v1/*";
        HttpsListenerHost = Environment.GetEnvironmentVariable("HTTPS_LISTENER_HOST");
        HttpsListenerPort = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PORT");
        KeystoreFilePath = Environment.GetEnvironmentVariable("KEYSTORE_FILE_PATH");
        KeystoreCertAlias = Environment.GetEnvironmentVariable("KEYSTORE_CERT_ALIAS");
        KeystoreKeypassword = Environment.GetEnvironmentVariable("KEYSTORE_KEYPASSWORD");
        KeystoreStorepassword = Environment.GetEnvironmentVariable("KEYSTORE_STOREPASSWORD");
        MuleEnv = Environment.GetEnvironmentVariable("MULE_ENV");
        JsonLoggerMaskedFields = Environment.GetEnvironmentVariable("JSON_LOGGER_MASKED_FIELDS");
        AutodiscoveryApiId = Environment.GetEnvironmentVariable("AUTODISCOVERY_API_ID");
        SalesforceUsername = Environment.GetEnvironmentVariable("SALESFORCE_USERNAME");
        SalesforcePassword = Environment.GetEnvironmentVariable("SALESFORCE_PASSWORD");
        SalesforceToken = Environment.GetEnvironmentVariable("SALESFORCE_TOKEN");
        SalesforceUrl = Environment.GetEnvironmentVariable("SALESFORCE_URL");
        AmazonS3Accesskey = Environment.GetEnvironmentVariable("AMAZON_S3_ACCESSKEY");
        AmazonS3Secretkey = Environment.GetEnvironmentVariable("AMAZON_S3_SECRETKEY");
        AmazonS3Region = Environment.GetEnvironmentVariable("AMAZON_S3_REGION");
        AmazonS3Bucket = Environment.GetEnvironmentVariable("AMAZON_S3_BUCKET");
        _awsSecretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
        _dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        _dbPort = Environment.GetEnvironmentVariable("DB_PORT");
        _dbName = Environment.GetEnvironmentVariable("DB_NAME");
        _dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
        _dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
    }

    public static Settings Instance => LazyInstance.Value;

    public string HttpsListenerPath { get; }
    public string? HttpsListenerHost { get; }
    public string? HttpsListenerPort { get; }
    public string? KeystoreFilePath { get; }
    public string? KeystoreCertAlias { get; }
    public string? KeystoreKeypassword { get; }
    public string? KeystoreStorepassword { get; }
    public string? MuleEnv { get; }
    public string? JsonLoggerMaskedFields { get; }
    public string? AutodiscoveryApiId { get; }
    public string? SalesforceUsername { get; }
    public string? SalesforcePassword { get; }
    public string? SalesforceToken { get; }
    public string? SalesforceUrl { get; }
    public string? AmazonS3Accesskey { get; }
    public string? AmazonS3Secretkey { get; }
    public string? AmazonS3Region { get; }
    public string? AmazonS3Bucket { get; }

    public async Task<string> GetConnectionStringAsync(CancellationToken cancellationToken)
    {
        if (_cachedConnectionString is not null)
        {
            return _cachedConnectionString;
        }

        string connectionString;
        if (!string.IsNullOrWhiteSpace(_dbHost) &&
            !string.IsNullOrWhiteSpace(_dbPort) &&
            !string.IsNullOrWhiteSpace(_dbName) &&
            !string.IsNullOrWhiteSpace(_dbUsername) &&
            !string.IsNullOrWhiteSpace(_dbPassword))
        {
            connectionString = BuildConnectionString(_dbHost, _dbPort, _dbName, _dbUsername, _dbPassword);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_awsSecretName))
            {
                throw new InvalidOperationException("Database configuration is missing");
            }

            var secret = await LoadSecretAsync(_awsSecretName, cancellationToken);
            connectionString = BuildConnectionString(secret.Host, secret.Port, secret.DbName, secret.Username, secret.Password);
        }

        lock (_lock)
        {
            _cachedConnectionString ??= connectionString;
        }

        return _cachedConnectionString;
    }

    private static string BuildConnectionString(string host, string port, string dbName, string username, string password)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.TryParse(port, out var parsedPort) ? parsedPort : 5432,
            Database = dbName,
            Username = username,
            Password = password,
            Timeout = 30,
            CommandTimeout = 30,
            Pooling = true
        };

        return builder.ConnectionString;
    }

    private static async Task<DatabaseSecret> LoadSecretAsync(string secretName, CancellationToken cancellationToken)
    {
        using var client = new AmazonSecretsManagerClient();
        var response = await client.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = secretName
        }, cancellationToken);

        using var document = JsonDocument.Parse(response.SecretString);
        var root = document.RootElement;

        return new DatabaseSecret
        {
            Username = SecretHelper.GetSecretText(root, "username"),
            Password = SecretHelper.GetSecretText(root, "password"),
            Host = SecretHelper.GetSecretText(root, "host"),
            Port = SecretHelper.GetSecretText(root, "port"),
            DbName = SecretHelper.GetSecretText(root, "dbname")
        };
    }
}