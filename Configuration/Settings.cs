using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace BusTravelAccountsSaLambda;

public sealed class Settings
{
    private static readonly object Sync = new();
    private static DatabaseSettings? CachedDatabaseSettings;
    private static bool CachedDatabaseSettingsLoaded;

    public string ListenerPath { get; }

    public Settings()
    {
        ListenerPath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH") ?? "api/v1/*";
    }

    public DatabaseSettings GetDatabaseSettings()
    {
        if (CachedDatabaseSettingsLoaded)
        {
            return CachedDatabaseSettings!;
        }

        lock (Sync)
        {
            if (CachedDatabaseSettingsLoaded)
            {
                return CachedDatabaseSettings!;
            }

            DatabaseSettings? settings = null;
            var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");

            if (!string.IsNullOrWhiteSpace(secretName))
            {
                try
                {
                    using var client = new AmazonSecretsManagerClient();
                    var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                    if (!string.IsNullOrWhiteSpace(response.SecretString))
                    {
                        settings = DatabaseSettings.FromSecretJson(response.SecretString);
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Error("Failed to load database credentials from Secrets Manager", ex);
                }
            }

            settings ??= DatabaseSettings.FromEnvironment();
            CachedDatabaseSettings = settings;
            CachedDatabaseSettingsLoaded = true;
            return settings;
        }
    }
}

public sealed class DatabaseSettings
{
    public string Username { get; }
    public string Password { get; }
    public string Host { get; }
    public string Database { get; }
    public int Port { get; }

    private DatabaseSettings(string username, string password, string host, string database, int port)
    {
        Username = username;
        Password = password;
        Host = host;
        Database = database;
        Port = port;
    }

    public static DatabaseSettings FromSecretJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return new DatabaseSettings(
            ReadText(root, "username"),
            ReadText(root, "password"),
            ReadText(root, "host"),
            ReadText(root, "dbname"),
            int.Parse(ReadText(root, "port")));
    }

    public static DatabaseSettings FromEnvironment()
    {
        return new DatabaseSettings(
            Environment.GetEnvironmentVariable("DB_USERNAME") ?? string.Empty,
            Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty,
            Environment.GetEnvironmentVariable("DB_HOST") ?? string.Empty,
            Environment.GetEnvironmentVariable("DB_NAME") ?? string.Empty,
            int.Parse(Environment.GetEnvironmentVariable("DB_PORT") ?? "0"));
    }

    private static string ReadText(JsonElement root, string name)
    {
        var value = root.GetProperty(name);
        return value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText();
    }
}
