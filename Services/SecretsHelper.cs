using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace TestCsharpLambdaTc123Lambda.Services;

public static class SecretsHelper
{
    private static readonly object Sync = new();
    private static Dictionary<string, string>? Cache;

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            var cache = Cache;
            if (cache is null)
            {
                lock (Sync)
                {
                    cache = Cache;
                    if (cache is null)
                    {
                        Cache = cache = LoadSecrets();
                    }
                }
            }

            if (cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
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

    private static Dictionary<string, string> LoadSecrets()
    {
        try
        {
            var secretName = Environment.GetEnvironmentVariable("SECRET_NAME") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(secretName))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            using var client = new AmazonSecretsManagerClient();
            var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in dict.Keys)
            {
                Console.WriteLine(key);
            }
            return dict;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SecretsHelper load failed: {ex.Message}");
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}