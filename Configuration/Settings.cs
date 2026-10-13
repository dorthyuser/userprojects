using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace BusTravelAccountsSaLambda;

public sealed class Settings
{
    private readonly Lazy<Task<DatabaseSettings?>> _secretSettings;

    public Settings()
    {
        ListenerPath = GetEnvironmentVariable("HTTPS_LISTENER_PATH") ?? "api/v1/*";
        AwsSecretName = GetEnvironmentVariable("AWS_SECRET_NAME");
        DbHost = GetEnvironmentVariable("DB_HOST");
        DbPort = GetEnvironmentVariable("DB_PORT");
        DbName = GetEnvironmentVariable("DB_NAME");
        DbUsername = GetEnvironmentVariable("DB_USERNAME");
        DbPassword = GetEnvironmentVariable("DB_PASSWORD");
        _secretSettings = new Lazy<Task<DatabaseSettings?>>(LoadSecretAsync);
    }

    public string ListenerPath { get; }
    public string? AwsSecretName { get; }
    public string? DbHost { get; }
    public string? DbPort { get; }
    public string? DbName { get; }
    public string? DbUsername { get; }
    public string? DbPassword { get; }

    public async Task<DatabaseSettings> GetDatabaseSettingsAsync()
    {
        if (!string.IsNullOrWhiteSpace(AwsSecretName))
        {
            var secret = await _secretSettings.Value;
            if (secret is not null)
            {
                return secret;
            }
        }

        return new DatabaseSettings(
            Require(DbHost, "DB_HOST"),
            int.Parse(Require(DbPort, "DB_PORT")),
            Require(DbName, "DB_NAME"),
            Require(DbUsername, "DB_USERNAME"),
            Require(DbPassword, "DB_PASSWORD"));
    }

    private async Task<DatabaseSettings?> LoadSecretAsync()
    {
        if (string.IsNullOrWhiteSpace(AwsSecretName))
        {
            return null;
        }

        using var client = new AmazonSecretsManagerClient();
        var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = AwsSecretName });
        if (string.IsNullOrWhiteSpace(response.SecretString))
        {
            return null;
        }

        using var document = JsonDocument.Parse(response.SecretString);
        var root = document.RootElement;
        var host = root.GetProperty("host").GetString() ?? string.Empty;
        var portText = root.GetProperty("port").ValueKind == JsonValueKind.String ? root.GetProperty("port").GetString() : root.GetProperty("port").GetRawText();
        var dbName = root.GetProperty("dbname").GetString() ?? string.Empty;
        var username = root.GetProperty("username").GetString() ?? string.Empty;
        var password = root.GetProperty("password").GetString() ?? string.Empty;
        return new DatabaseSettings(host, int.Parse(portText ?? "0"), dbName, username, password);
    }

    private static string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);
    private static string Require(string? value, string name) => !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"Missing environment variable {name}");
}

public sealed record DatabaseSettings(string Host, int Port, string Database, string Username, string Password);
