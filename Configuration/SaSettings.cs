using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace BusTravelAccountsLambda;

public sealed class SaSettings
{
    private static readonly Lazy<SaSettings> Lazy = new(Create);
    public static SaSettings Instance => Lazy.Value;

    public SaDbSettings Db { get; }

    private SaSettings(SaDbSettings db)
    {
        Db = db;
    }

    private static SaSettings Create()
    {
        var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
        if (!string.IsNullOrWhiteSpace(secretName))
        {
            try
            {
                var client = new AmazonSecretsManagerClient();
                var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(response.SecretString))
                {
                    var doc = JsonDocument.Parse(response.SecretString);
                    var portText = GetString(doc.RootElement, "port") ?? GetFallback("DB_PORT");
                    return new SaSettings(new SaDbSettings(
                        GetString(doc.RootElement, "username") ?? GetFallback("DB_USERNAME"),
                        GetString(doc.RootElement, "password") ?? GetFallback("DB_PASSWORD"),
                        GetString(doc.RootElement, "host") ?? GetFallback("DB_HOST"),
                        GetString(doc.RootElement, "dbname") ?? GetFallback("DB_NAME"),
                        int.TryParse(portText, out var parsedPort) ? parsedPort : 5432));
                }
            }
            catch
            {
            }
        }

        var fallbackPortText = GetFallback("DB_PORT");
        return new SaSettings(new SaDbSettings(
            GetFallback("DB_USERNAME"),
            GetFallback("DB_PASSWORD"),
            GetFallback("DB_HOST"),
            GetFallback("DB_NAME"),
            int.TryParse(fallbackPortText, out var fallbackPort) ? fallbackPort : 5432));
    }

    private static string? GetFallback(string name) => Environment.GetEnvironmentVariable(name);

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
    }
}

public sealed record SaDbSettings(string Username, string Password, string Host, string Database, int Port);