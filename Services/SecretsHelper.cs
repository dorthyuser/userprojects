using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace DemoTravelcardsLambda.Services;

public static class SecretsHelper
{
    private static readonly object Sync = new();
    private static Dictionary<string, string>? _cache;

    public static string Get(string secretKey, string envFallback)
    {
        EnsureLoaded();
        if (_cache is not null && _cache.TryGetValue(secretKey, out var val) && !string.IsNullOrWhiteSpace(val)) return val;
        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }

    private static void EnsureLoaded()
    {
        if (_cache is not null) return;
        lock (Sync)
        {
            if (_cache is not null) return;
            var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
            if (string.IsNullOrWhiteSpace(secretName))
            {
                _cache = new Dictionary<string, string>();
                return;
            }
            try
            {
                using var client = new AmazonSecretsManagerClient();
                var resp = Task.Run(() => client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName })).GetAwaiter().GetResult();
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
            }
            catch
            {
                _cache = new Dictionary<string, string>();
            }
        }
    }
}