using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace travelcardcsharpsb1114.Services;

public static class SecretsHelper
{
    private static readonly object LockObject = new();
    private static Dictionary<string, string>? _cache;

    public static string Get(string secretKey, string envFallback)
    {
        EnsureLoaded();
        if (_cache != null && _cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }

    private static void EnsureLoaded()
    {
        if (_cache != null) return;
        lock (LockObject)
        {
            if (_cache != null) return;
            try
            {
                var secretName = Environment.GetEnvironmentVariable("SECRET_NAME");
                if (string.IsNullOrWhiteSpace(secretName))
                {
                    _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    return;
                }
                using var client = new AmazonSecretsManagerClient();
                var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}