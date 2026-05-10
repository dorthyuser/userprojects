using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace Travelcardcsharp1008Lambda.Services;

public static class SecretsHelper
{
    private static Dictionary<string, string>? _cache;
    private static readonly object _lock = new();

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            var cache = _cache;
            if (cache is null)
            {
                lock (_lock)
                {
                    cache = _cache;
                    if (cache is null)
                    {
                        var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
                        if (!string.IsNullOrWhiteSpace(secretName))
                        {
                            using var client = new AmazonSecretsManagerClient();
                            var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                            cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
                            _cache = cache;
                            foreach (var key in cache.Keys)
                            {
                                Console.WriteLine(key);
                            }
                        }
                        else
                        {
                            cache = new Dictionary<string, string>();
                            _cache = cache;
                        }
                    }
                }
            }

            if (cache is not null && cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SecretsHelper error: {ex.Message}");
        }

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
}