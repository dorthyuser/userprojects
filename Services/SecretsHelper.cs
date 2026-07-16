namespace Csharpae1012Lambda.Services;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

public static class SecretsHelper
{
    private static readonly object _lock = new();
    private static Dictionary<string, string>? _cache;

    public static string Get(string secretKey, string envFallback)
    {
        EnsureLoaded();
        if (_cache != null && _cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
            return value;
        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }

    private static void EnsureLoaded()
    {
        if (_cache != null) return;
        lock (_lock)
        {
            if (_cache != null) return;
            var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
            if (string.IsNullOrWhiteSpace(secretName))
            {
                _cache = new Dictionary<string, string>();
                return;
            }
            try
            {
                var client = new AmazonSecretsManagerClient();
                var resp = Task.Run(() => client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName })).GetAwaiter().GetResult();
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
                foreach (var key in _cache.Keys) Console.WriteLine(key);
            }
            catch
            {
                _cache = new Dictionary<string, string>();
            }
        }
    }
}