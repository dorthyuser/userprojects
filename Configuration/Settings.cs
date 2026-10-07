using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
namespace BusTravelAccountsSaMainLambda;

public sealed class Settings
{
    private static readonly object SecretLock = new();
    private static Settings? Cached;

    public string ListenerPath { get; init; } = string.Empty;
    public string ConnectionString { get; init; } = string.Empty;
    public string? MuleEnv { get; init; }
    public string? JsonLoggerMaskedFields { get; init; }
    public string? JsonLoggerApplicationName { get; init; }
    public string? JsonLoggerApplicationVersion { get; init; }

    public static Settings Load()
    {
        lock (SecretLock)
        {
            if (Cached is not null)
            {
                return Cached;
            }

            var listenerPath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH") ?? "api/v1/*";
            var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
            var dbName = Environment.GetEnvironmentVariable("DB_NAME");
            var dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

            string host;
            string port;
            string name;
            string username;
            string password;

            if (!string.IsNullOrWhiteSpace(dbHost) && !string.IsNullOrWhiteSpace(dbPort) && !string.IsNullOrWhiteSpace(dbName) && !string.IsNullOrWhiteSpace(dbUsername) && !string.IsNullOrWhiteSpace(dbPassword))
            {
                host = dbHost;
                port = dbPort;
                name = dbName;
                username = dbUsername;
                password = dbPassword;
            }
            else if (!string.IsNullOrWhiteSpace(secretName))
            {
                var secretJson = LoadSecret(secretName);
                using var doc = JsonDocument.Parse(secretJson);
                host = doc.RootElement.GetProperty("host").GetString() ?? string.Empty;
                port = doc.RootElement.GetProperty("port").GetString() ?? string.Empty;
                name = doc.RootElement.GetProperty("dbname").GetString() ?? string.Empty;
                username = doc.RootElement.GetProperty("username").GetString() ?? string.Empty;
                password = doc.RootElement.GetProperty("password").GetString() ?? string.Empty;
            }
            else
            {
                throw new InvalidOperationException("Database configuration not found.");
            }

            Cached = new Settings
            {
                ListenerPath = listenerPath,
                ConnectionString = $"Host={host};Port={port};Database={name};Username={username};Password={password};Pooling=true;Maximum Pool Size=10;Timeout=15;Command Timeout=60",
                MuleEnv = Environment.GetEnvironmentVariable("MULE_ENV"),
                JsonLoggerMaskedFields = Environment.GetEnvironmentVariable("JSON_LOGGER_MASKED_FIELDS"),
                JsonLoggerApplicationName = Environment.GetEnvironmentVariable("JSON_LOGGER_APPLICATION_NAME"),
                JsonLoggerApplicationVersion = Environment.GetEnvironmentVariable("JSON_LOGGER_APPLICATION_VERSION")
            };

            return Cached;
        }
    }

    private static string LoadSecret(string secretName)
    {
        using var client = new AmazonSecretsManagerClient();
        var response = client.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = secretName
        }).GetAwaiter().GetResult();

        return response.SecretString ?? string.Empty;
    }
}