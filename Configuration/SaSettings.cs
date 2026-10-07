using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace MuleaesaMainLambda;

public sealed class SaSettings
{
    private static readonly object SyncRoot = new();
    private static SaSettings? cached;

    public string ListenerPath { get; }
    public string? DbHost { get; }
    public int? DbPort { get; }
    public string? DbName { get; }
    public string? DbUsername { get; }
    public string? DbPassword { get; }
    public string? AwsSecretName { get; }
    public string ConnectionString { get; }

    private SaSettings(string listenerPath, string? dbHost, int? dbPort, string? dbName, string? dbUsername, string? dbPassword, string? awsSecretName)
    {
        ListenerPath = listenerPath;
        DbHost = dbHost;
        DbPort = dbPort;
        DbName = dbName;
        DbUsername = dbUsername;
        DbPassword = dbPassword;
        AwsSecretName = awsSecretName;
        ConnectionString = BuildConnectionString();
    }

    public static SaSettings Current()
    {
        if (cached is not null)
        {
            return cached;
        }

        lock (SyncRoot)
        {
            if (cached is not null)
            {
                return cached;
            }

            var listenerPath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH") ?? "api/v1/*";
            var awsSecretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");

            string? dbHost = Environment.GetEnvironmentVariable("DB_HOST");
            string? dbPortRaw = Environment.GetEnvironmentVariable("DB_PORT");
            string? dbName = Environment.GetEnvironmentVariable("DB_NAME");
            string? dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
            string? dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

            if (string.IsNullOrWhiteSpace(dbHost) || string.IsNullOrWhiteSpace(dbPortRaw) || string.IsNullOrWhiteSpace(dbName) || string.IsNullOrWhiteSpace(dbUsername) || string.IsNullOrWhiteSpace(dbPassword))
            {
                if (!string.IsNullOrWhiteSpace(awsSecretName))
                {
                    var secret = LoadSecretAsync(awsSecretName).GetAwaiter().GetResult();
                    dbHost ??= secret.Host;
                    dbPortRaw ??= secret.Port;
                    dbName ??= secret.DbName;
                    dbUsername ??= secret.Username;
                    dbPassword ??= secret.Password;
                }
            }

            var dbPort = int.TryParse(dbPortRaw, out var parsedPort) ? parsedPort : (int?)null;
            cached = new SaSettings(listenerPath, dbHost, dbPort, dbName, dbUsername, dbPassword, awsSecretName);
            return cached;
        }
    }

    private string BuildConnectionString()
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = DbHost ?? string.Empty,
            Port = DbPort ?? 5432,
            Database = DbName ?? string.Empty,
            Username = DbUsername ?? string.Empty,
            Password = DbPassword ?? string.Empty,
            Pooling = true,
            IncludeErrorDetail = true
        };

        return builder.ConnectionString;
    }

    private static async Task<SecretPayload> LoadSecretAsync(string secretName)
    {
        using var client = new AmazonSecretsManagerClient();
        var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).ConfigureAwait(false);
        var json = response.SecretString ?? string.Empty;
        var payload = JsonSerializer.Deserialize<SecretPayload>(json, JsonOptions.Default) ?? new SecretPayload();
        return payload;
    }

    private sealed class SecretPayload
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Host { get; set; }
        public string? Port { get; set; }
        public string? DbName { get; set; }
    }
}
