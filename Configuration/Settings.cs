using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace BusTravelAccountsSaLambda;

public sealed class Settings
{
    private static readonly Lazy<Settings> LazyInstance = new(LoadFromEnvironment);
    private readonly Lazy<DatabaseSettings> databaseSettings;

    private Settings(string basePath, string? secretName, string? dbHost, int? dbPort, string? dbName, string? dbUsername, string? dbPassword)
    {
        BasePath = basePath;
        SecretName = secretName;
        DbHost = dbHost;
        DbPort = dbPort;
        DbName = dbName;
        DbUsername = dbUsername;
        DbPassword = dbPassword;
        databaseSettings = new Lazy<DatabaseSettings>(ResolveDatabaseSettings);
    }

    public string BasePath { get; }
    public string? SecretName { get; }
    public string? DbHost { get; }
    public int? DbPort { get; }
    public string? DbName { get; }
    public string? DbUsername { get; }
    public string? DbPassword { get; }

    public DatabaseSettings Database => databaseSettings.Value;

    public static Settings Load() => LazyInstance.Value;

    private static Settings LoadFromEnvironment()
    {
        var basePath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH") ?? "api/v1/*";
        var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        var dbPortText = Environment.GetEnvironmentVariable("DB_PORT");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        int? dbPort = int.TryParse(dbPortText, out var parsedPort) ? parsedPort : null;
        return new Settings(basePath, secretName, dbHost, dbPort, dbName, dbUsername, dbPassword);
    }

    private DatabaseSettings ResolveDatabaseSettings()
    {
        if (!string.IsNullOrWhiteSpace(SecretName))
        {
            using var client = new AmazonSecretsManagerClient();
            var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = SecretName }).GetAwaiter().GetResult();
            var secretJson = response.SecretString ?? string.Empty;
            using var document = JsonDocument.Parse(secretJson);
            var root = document.RootElement;
            var username = root.GetProperty("username").GetString() ?? string.Empty;
            var password = root.GetProperty("password").GetString() ?? string.Empty;
            var host = root.GetProperty("host").GetString() ?? string.Empty;
            var portText = root.GetProperty("port").ValueKind == JsonValueKind.String ? root.GetProperty("port").GetString() : root.GetProperty("port").GetRawText();
            var dbname = root.GetProperty("dbname").ValueKind == JsonValueKind.String ? root.GetProperty("dbname").GetString() : root.GetProperty("dbname").GetRawText();
            return new DatabaseSettings(host, int.Parse(portText ?? "5432"), dbname ?? string.Empty, username, password);
        }

        return new DatabaseSettings(DbHost ?? string.Empty, DbPort ?? 5432, DbName ?? string.Empty, DbUsername ?? string.Empty, DbPassword ?? string.Empty);
    }
}

public sealed record DatabaseSettings(string Host, int Port, string Database, string Username, string Password);
