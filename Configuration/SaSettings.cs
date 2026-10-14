using System;
using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace BusTravelAccountsLambda;

public sealed class SaSettings
{
    private static readonly object Sync = new();
    private static SaSettings? cached;

    public string ConnectionString { get; }

    public SaSettings()
    {
        lock (Sync)
        {
            if (cached != null)
            {
                ConnectionString = cached.ConnectionString;
                return;
            }

            ConnectionString = BuildConnectionString();
            cached = this;
        }
    }

    private static string BuildConnectionString()
    {
        var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
        if (!string.IsNullOrWhiteSpace(secretName))
        {
            var client = new AmazonSecretsManagerClient();
            var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
            var secret = JsonDocument.Parse(response.SecretString ?? throw new InvalidOperationException("SecretString missing"));
            var root = secret.RootElement;
            var host = ReadText(root, "host");
            var port = ReadText(root, "port");
            var dbName = ReadText(root, "dbname");
            var username = ReadText(root, "username");
            var password = ReadText(root, "password");
            return Build(host, port, dbName, username, password);
        }

        var localHost = Environment.GetEnvironmentVariable("DB_HOST") ?? throw new InvalidOperationException("DB_HOST missing");
        var localPort = Environment.GetEnvironmentVariable("DB_PORT") ?? throw new InvalidOperationException("DB_PORT missing");
        var localDb = Environment.GetEnvironmentVariable("DB_NAME") ?? throw new InvalidOperationException("DB_NAME missing");
        var localUser = Environment.GetEnvironmentVariable("DB_USERNAME") ?? throw new InvalidOperationException("DB_USERNAME missing");
        var localPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? throw new InvalidOperationException("DB_PASSWORD missing");
        return Build(localHost, localPort, localDb, localUser, localPassword);
    }

    private static string ReadText(JsonElement root, string name)
    {
        var value = root.GetProperty(name);
        return value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText();
    }

    private static string Build(string host, string port, string dbName, string username, string password)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.Parse(port),
            Database = dbName,
            Username = username,
            Password = password,
            SslMode = Npgsql.SslMode.Require,
            TrustServerCertificate = false
        };
        return builder.ConnectionString;
    }
}
