using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace Travelcardcharplambda1031Lambda.Services;

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
            Console.WriteLine($"SecretsHelper error: {ex.Message}");
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
                var secretName = Environment.GetEnvironmentVariable("AWS_SECRET") ?? string.Empty;
                using var client = new AmazonSecretsManagerClient();
                var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
                foreach (var key in _cache.Keys)
                {
                    Console.WriteLine(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SecretsHelper load error: {ex.Message}");
                _cache = new Dictionary<string, string>();
            }
        }
    }
}