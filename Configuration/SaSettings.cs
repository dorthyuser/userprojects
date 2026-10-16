using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace BusTravelAccountsLambda;

public sealed class SaSettings
{
    private static readonly Lazy<SaSettings> LazyInstance = new(Load);
    private readonly string _connectionString;

    private SaSettings(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static SaSettings Instance => LazyInstance.Value;

    public string ConnectionString => _connectionString;

    private static SaSettings Load()
    {
        var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
        if (!string.IsNullOrWhiteSpace(secretName))
        {
            var client = new AmazonSecretsManagerClient();
            var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
            var secretJson = response.SecretString ?? throw new InvalidOperationException("SecretString missing");
            using var document = JsonDocument.Parse(secretJson);
            var root = document.RootElement;
            var host = GetText(root, "host");
            var port = GetText(root, "port");
            var dbName = GetText(root, "dbname");
            var username = GetText(root, "username");
            var password = GetText(root, "password");
            return new SaSettings(BuildConnectionString(host, port, dbName, username, password));
        }

        var localHost = Environment.GetEnvironmentVariable("DB_HOST") ?? throw new InvalidOperationException("DB_HOST missing");
        var localPort = Environment.GetEnvironmentVariable("DB_PORT") ?? throw new InvalidOperationException("DB_PORT missing");
        var localDb = Environment.GetEnvironmentVariable("DB_NAME") ?? throw new InvalidOperationException("DB_NAME missing");
        var localUser = Environment.GetEnvironmentVariable("DB_USERNAME") ?? throw new InvalidOperationException("DB_USERNAME missing");
        var localPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? throw new InvalidOperationException("DB_PASSWORD missing");
        return new SaSettings(BuildConnectionString(localHost, localPort, localDb, localUser, localPassword));
    }

    private static string GetText(JsonElement root, string name)
    {
        var element = root.GetProperty(name);
        return element.ValueKind == JsonValueKind.String ? element.GetString()! : element.GetRawText();
    }

    private static string BuildConnectionString(string host, string port, string dbName, string username, string password)
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