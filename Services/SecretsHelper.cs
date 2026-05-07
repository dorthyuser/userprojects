using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace Travelcardchsarplambda1050Lambda.Services;

public static class SecretsHelper
{
    private static readonly object _lock = new();
    private static Dictionary<string, string>? _cache;

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            var cache = LoadCache();
            if (cache != null && cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
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

    private static Dictionary<string, string>? LoadCache()
    {
        if (_cache is not null) return _cache;
        lock (_lock)
        {
            if (_cache is not null) return _cache;
            try
            {
                var secretName = Environment.GetEnvironmentVariable("SECRET_NAME");
                if (string.IsNullOrWhiteSpace(secretName))
                {
                    _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    return _cache;
                }

                using var client = new AmazonSecretsManagerClient();
                var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SecretsHelper load error: {ex.Message}");
                _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }
        return _cache;
    }
}