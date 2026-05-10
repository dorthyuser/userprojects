using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace Dashboard2Lambda.Services;

public static class SecretsHelper
{
    private static readonly object LockObj = new();
    private static Dictionary<string, string>? _cache;

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            var cache = _cache;
            if (cache is null)
            {
                lock (LockObj)
                {
                    cache = _cache;
                    if (cache is null)
                    {
                        cache = LoadSecrets();
                        _cache = cache;
                    }
                }
            }

            if (cache is not null && cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
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
        var secretName = Environment.GetEnvironmentVariable("SECRET_NAME");
        if (string.IsNullOrWhiteSpace(secretName))
        {
            return new Dictionary<string, string>();
        }

        using var client = new AmazonSecretsManagerClient();
        var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
        foreach (var key in dict.Keys)
        {
            Console.WriteLine(key);
        }
        return dict;
    }
}
