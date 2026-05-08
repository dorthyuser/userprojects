using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace DemoTravelcardClincalLambda.Services;

public static class SecretsHelper
{
    private static Dictionary<string, string>? _cache;
    private static readonly object _lock = new();

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            EnsureLoaded();
            if (_cache is not null && _cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }
        catch
        {
        }

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }

    private static void EnsureLoaded()
    {
        if (_cache is not null) return;
        lock (_lock)
        {
            if (_cache is not null) return;
            try
            {
                var secretName = Environment.GetEnvironmentVariable("AWS_SECRET") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(secretName))
                {
                    _cache = new Dictionary<string, string>();
                    return;
                }

                using var client = new AmazonSecretsManagerClient();
                var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
            }
            catch
            {
                _cache = new Dictionary<string, string>();
            }
        }
    }
}