using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace Travelcardcharplambda225Lambda.Services;

public static class SecretsHelper
{
    private static readonly object LockObj = new();
    private static Dictionary<string, string>? Cache;

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            EnsureLoaded();
            if (Cache is not null && Cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
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
        if (Cache is not null) return;
        lock (LockObj)
        {
            if (Cache is not null) return;
            try
            {
                var secretId = Environment.GetEnvironmentVariable("AWS_SECRET") ?? string.Empty;
                using var client = new AmazonSecretsManagerClient();
                var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId }).GetAwaiter().GetResult();
                Cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
                foreach (var key in Cache.Keys)
                {
                    Console.WriteLine(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SecretsHelper load failed: {ex.Message}");
                Cache = new Dictionary<string, string>();
            }
        }
    }
}