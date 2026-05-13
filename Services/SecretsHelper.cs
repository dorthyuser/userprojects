using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace Travelcardcsharp1017Lambda.Services;

public static class SecretsHelper
{
    private static Dictionary<string, string>? _cache;
    private static readonly object _lock = new();

    public static string Get(string secretKey, string envFallback)
    {
        var cache = EnsureCache();
        if (cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }

    private static Dictionary<string, string> EnsureCache()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        lock (_lock)
        {
            if (_cache is not null)
            {
                return _cache;
            }

            var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
            if (string.IsNullOrWhiteSpace(secretName))
            {
                _cache = new Dictionary<string, string>();
                return _cache;
            }

            try
            {
                var client = new AmazonSecretsManagerClient();
                var response = Task.Run(() => client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName })).GetAwaiter().GetResult();
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString ?? "{}") ?? new Dictionary<string, string>();
                foreach (var key in _cache.Keys)
                {
                    Console.WriteLine(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SecretsHelper error: {ex.Message}");
                _cache = new Dictionary<string, string>();
            }

            return _cache;
        }
    }
}