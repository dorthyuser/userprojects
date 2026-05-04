using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace Travelcardlambda1010Lambda.Services;

public static class SecretsHelper
{
    private static readonly object LockObj = new();
    private static Dictionary<string, string>? _cache;

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            EnsureLoaded();
            if (_cache is not null && _cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Secrets load error: {ex}");
        }

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }

    private static void EnsureLoaded()
    {
        if (_cache is not null)
        {
            return;
        }

        lock (LockObj)
        {
            if (_cache is not null)
            {
                return;
            }

            try
            {
                var secretName = Environment.GetEnvironmentVariable("SECRET_NAME") ?? string.Empty;
                using var client = new AmazonSecretsManagerClient();
                var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
                _cache = data;
                foreach (var key in _cache.Keys)
                {
                    Console.WriteLine(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Secret fetch failed: {ex}");
                _cache = new Dictionary<string, string>();
            }
        }
    }
}