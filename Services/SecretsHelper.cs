using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace Travelcardcsharplambda355Lambda.Services;

public static class SecretsHelper
{
    private static readonly object LockObj = new();
    private static Dictionary<string, string>? Cache;

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            var cache = Load();
            if (cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        catch
        {
        }

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }

    private static Dictionary<string, string> Load()
    {
        if (Cache != null) return Cache;
        lock (LockObj)
        {
            if (Cache != null) return Cache;
            try
            {
                var secretName = Environment.GetEnvironmentVariable("SECRET_NAME") ?? string.Empty;
                using var client = new AmazonSecretsManagerClient();
                var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                Cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
            }
            catch
            {
                Cache = new Dictionary<string, string>
                {
                    { "host", Environment.GetEnvironmentVariable("POSTGRESQLHOST") ?? string.Empty },
                    { "port", Environment.GetEnvironmentVariable("POSTGRESQLPORT") ?? string.Empty },
                    { "dbname", Environment.GetEnvironmentVariable("POSTGRESQLDATABASE") ?? string.Empty },
                    { "username", Environment.GetEnvironmentVariable("POSTGRESQLUSERNAME") ?? string.Empty },
                    { "password", Environment.GetEnvironmentVariable("POSTGRESQLPASSWORD") ?? string.Empty }
                };
            }
        }

        return Cache ?? new Dictionary<string, string>();
    }
}
