using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace BusTravelAccountsSaLambda;

public sealed class Settings
{
    public string BasePath { get; }
    public string DbHost { get; }
    public int DbPort { get; }
    public string DbName { get; }
    public string DbUsername { get; }
    public string DbPassword { get; }

    private Settings(string basePath, string dbHost, int dbPort, string dbName, string dbUsername, string dbPassword)
    {
        BasePath = basePath;
        DbHost = dbHost;
        DbPort = dbPort;
        DbName = dbName;
        DbUsername = dbUsername;
        DbPassword = dbPassword;
    }

    public static Settings Load()
    {
        var basePath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH");
        if (string.IsNullOrWhiteSpace(basePath))
        {
            basePath = "api/v1/*";
        }

        var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
        if (!string.IsNullOrWhiteSpace(secretName))
        {
            var client = new AmazonSecretsManagerClient();
            var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
            var secretJson = response.SecretString ?? string.Empty;
            using var document = JsonDocument.Parse(secretJson);
            var root = document.RootElement;
            return new Settings(
                basePath,
                GetSecretText(root, "host"),
                int.Parse(GetSecretText(root, "port")),
                GetSecretText(root, "dbname"),
                GetSecretText(root, "username"),
                GetSecretText(root, "password"));
        }

        var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? string.Empty;
        var dbPortText = Environment.GetEnvironmentVariable("DB_PORT") ?? string.Empty;
        var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? string.Empty;
        var dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME") ?? string.Empty;
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;
        return new Settings(basePath, dbHost, int.Parse(dbPortText), dbName, dbUsername, dbPassword);
    }

    private static string GetSecretText(JsonElement root, string name)
    {
        var value = root.GetProperty(name);
        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.GetRawText();
    }
}