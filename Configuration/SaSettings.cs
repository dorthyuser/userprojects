using System;
using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace BusTravelAccountsLambda;

public sealed class SaSettings
{
    private static readonly object Sync = new();
    private static SaDatabaseSettings? cached;

    public SaDatabaseSettings Database { get; }

    public SaSettings()
    {
        Database = GetDatabaseSettings();
    }

    private static SaDatabaseSettings GetDatabaseSettings()
    {
        if (cached is not null)
        {
            return cached;
        }

        lock (Sync)
        {
            if (cached is not null)
            {
                return cached;
            }

            var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
            if (!string.IsNullOrWhiteSpace(secretName))
            {
                try
                {
                    var client = new AmazonSecretsManagerClient();
                    var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                    var json = response.SecretString ?? "{}";
                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;
                    cached = new SaDatabaseSettings(
                        GetSecretValue(root, "host") ?? Environment.GetEnvironmentVariable("DB_HOST") ?? string.Empty,
                        GetSecretValue(root, "port") ?? Environment.GetEnvironmentVariable("DB_PORT") ?? string.Empty,
                        GetSecretValue(root, "dbname") ?? Environment.GetEnvironmentVariable("DB_NAME") ?? string.Empty,
                        GetSecretValue(root, "username") ?? Environment.GetEnvironmentVariable("DB_USERNAME") ?? string.Empty,
                        GetSecretValue(root, "password") ?? Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty);
                    return cached;
                }
                catch
                {
                }
            }

            cached = new SaDatabaseSettings(
                Environment.GetEnvironmentVariable("DB_HOST") ?? string.Empty,
                Environment.GetEnvironmentVariable("DB_PORT") ?? string.Empty,
                Environment.GetEnvironmentVariable("DB_NAME") ?? string.Empty,
                Environment.GetEnvironmentVariable("DB_USERNAME") ?? string.Empty,
                Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty);
            return cached;
        }
    }

    private static string? GetSecretValue(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
    }
}

public sealed record SaDatabaseSettings(string Host, string Port, string Database, string Username, string Password);