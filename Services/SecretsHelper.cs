using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace Travelcardlambdacsharp1133Lambda.Services;

public static class SecretsHelper
{
    private static Dictionary<string, string>? _cache;
    private static readonly object LockObj = new();

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            var cache = LoadCache();
            if (cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        }
        catch
        {
        }

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }

    private static Dictionary<string, string> LoadCache()
    {
        if (_cache is not null) return _cache;
        lock (LockObj)
        {
            if (_cache is not null) return _cache;
            try
            {
                var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME") ?? string.Empty;
                using var client = new AmazonSecretsManagerClient();
                var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
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
