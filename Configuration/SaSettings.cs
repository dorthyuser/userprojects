using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace MulecombineMainLambda;

public sealed class SaSettings
{
    private static readonly Lazy<SaSettings> LazyInstance = new(Load);
    private static readonly object SecretLock = new();
    private static JsonDocument? SecretDocument;

    public string ListenerPath { get; }
    public string? DbHost { get; }
    public int? DbPort { get; }
    public string? DbName { get; }
    public string? DbUsername { get; }
    public string? DbPassword { get; }
    public string? AwsSecretName { get; }

    private SaSettings(string listenerPath, string? dbHost, int? dbPort, string? dbName, string? dbUsername, string? dbPassword, string? awsSecretName)
    {
        ListenerPath = listenerPath;
        DbHost = dbHost;
        DbPort = dbPort;
        DbName = dbName;
        DbUsername = dbUsername;
        DbPassword = dbPassword;
        AwsSecretName = awsSecretName;
    }

    public static SaSettings Current => LazyInstance.Value;

    public static string ResolveListenerPath() => Current.ListenerPath;

    public static (string Host, int Port, string Database, string Username, string Password) GetDatabaseConnection()
    {
        var settings = Current;
        if (!string.IsNullOrWhiteSpace(settings.DbHost) && settings.DbPort.HasValue && !string.IsNullOrWhiteSpace(settings.DbName) && !string.IsNullOrWhiteSpace(settings.DbUsername) && !string.IsNullOrWhiteSpace(settings.DbPassword))
        {
            return (settings.DbHost!, settings.DbPort.Value, settings.DbName!, settings.DbUsername!, settings.DbPassword!);
        }

        var secretName = settings.AwsSecretName;
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new InvalidOperationException("Database configuration is missing.");
        }

        var secret = GetSecretDocument(secretName!);
        var root = secret.RootElement;
        var host = ReadSecretText(root, "host") ?? throw new InvalidOperationException("Database configuration is missing host.");
        var portText = ReadSecretText(root, "port") ?? throw new InvalidOperationException("Database configuration is missing port.");
        var dbname = ReadSecretText(root, "dbname") ?? throw new InvalidOperationException("Database configuration is missing dbname.");
        var username = ReadSecretText(root, "username") ?? throw new InvalidOperationException("Database configuration is missing username.");
        var password = ReadSecretText(root, "password") ?? throw new InvalidOperationException("Database configuration is missing password.");
        return (host, int.Parse(portText, System.Globalization.CultureInfo.InvariantCulture), dbname, username, password);
    }

    private static SaSettings Load()
    {
        var listenerPath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH");
        if (string.IsNullOrWhiteSpace(listenerPath))
        {
            listenerPath = "api/v1/*";
        }

        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        var dbPortText = Environment.GetEnvironmentVariable("DB_PORT");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var awsSecretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");

        int? dbPort = null;
        if (!string.IsNullOrWhiteSpace(dbPortText) && int.TryParse(dbPortText, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsedPort))
        {
            dbPort = parsedPort;
        }

        return new SaSettings(listenerPath, dbHost, dbPort, dbName, dbUsername, dbPassword, awsSecretName);
    }

    private static JsonDocument GetSecretDocument(string secretName)
    {
        lock (SecretLock)
        {
            if (SecretDocument is not null)
            {
                return SecretDocument;
            }

            using var client = new AmazonSecretsManagerClient();
            var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
            var secretString = response.SecretString ?? string.Empty;
            SecretDocument = JsonDocument.Parse(secretString);
            return SecretDocument;
        }
    }

    private static string? ReadSecretText(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText().Trim('"');
    }
}