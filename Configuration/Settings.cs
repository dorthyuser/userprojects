using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace BusTravelAccountsSaLambda;

public sealed class Settings
{
    private static readonly object Sync = new();
    private static Settings? _current;
    private static Npgsql.NpgsqlConnectionStringBuilder? _connectionStringBuilder;

    public string ListenerPath { get; }
    public string? AwsSecretName { get; }
    public string? DbUsername { get; }
    public string? DbPassword { get; }
    public string? DbHost { get; }
    public string? DbName { get; }
    public string? DbPort { get; }

    private Settings()
    {
        ListenerPath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH") ?? "api/v1/*";
        AwsSecretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
        DbUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
        DbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        DbHost = Environment.GetEnvironmentVariable("DB_HOST");
        DbName = Environment.GetEnvironmentVariable("DB_NAME");
        DbPort = Environment.GetEnvironmentVariable("DB_PORT");
    }

    public static Settings Current
    {
        get
        {
            lock (Sync)
            {
                _current ??= new Settings();
                return _current;
            }
        }
    }

    public async Task<string> GetConnectionStringAsync(CancellationToken cancellationToken)
    {
        lock (Sync)
        {
            if (_connectionStringBuilder is not null)
            {
                return _connectionStringBuilder.ConnectionString;
            }
        }

        var credentials = await LoadCredentialsAsync(cancellationToken).ConfigureAwait(false);
        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = credentials.Host,
            Username = credentials.Username,
            Password = credentials.Password,
            Database = credentials.Database,
            Port = credentials.Port,
            Pooling = true,
            SslMode = Npgsql.SslMode.Require,
            TrustServerCertificate = false
        };

        lock (Sync)
        {
            _connectionStringBuilder ??= builder;
            return _connectionStringBuilder.ConnectionString;
        }
    }

    private async Task<DbCredentials> LoadCredentialsAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(AwsSecretName))
        {
            try
            {
                using var client = new AmazonSecretsManagerClient();
                var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = AwsSecretName }, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(response.SecretString))
                {
                    var doc = JsonDocument.Parse(response.SecretString);
                    var root = doc.RootElement;
                    var username = ReadSecretValue(root, "username");
                    var password = ReadSecretValue(root, "password");
                    var host = ReadSecretValue(root, "host");
                    var dbname = ReadSecretValue(root, "dbname");
                    var port = ReadSecretValue(root, "port");
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(dbname) && !string.IsNullOrWhiteSpace(port) && int.TryParse(port, out var parsedPort))
                    {
                        return new DbCredentials(username!, password!, host!, dbname!, parsedPort);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("Failed to load database credentials from Secrets Manager", ex);
            }
        }

        if (string.IsNullOrWhiteSpace(DbUsername) || string.IsNullOrWhiteSpace(DbPassword) || string.IsNullOrWhiteSpace(DbHost) || string.IsNullOrWhiteSpace(DbName) || string.IsNullOrWhiteSpace(DbPort) || !int.TryParse(DbPort, out var fallbackPort))
        {
            throw new InvalidOperationException("Database credentials are not configured.");
        }

        return new DbCredentials(DbUsername, DbPassword, DbHost, DbName, fallbackPort);
    }

    private static string? ReadSecretValue(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
    }

    private sealed record DbCredentials(string Username, string Password, string Host, string Database, int Port);
}