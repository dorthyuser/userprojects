using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
namespace Csharpae1140Lambda.Services;
public static class SecretsHelper
{
    private static readonly object Sync = new();
    private static Dictionary<string, string>? _cache;
    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            EnsureCache();
            if (_cache is not null && _cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        }
        catch { }
        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
    private static void EnsureCache()
    {
        if (_cache is not null) return;
        lock (Sync)
        {
            if (_cache is not null) return;
            var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
            if (string.IsNullOrWhiteSpace(secretName))
            {
                _cache = new Dictionary<string, string>();
                return;
            }
            try
            {
                using var client = new AmazonSecretsManagerClient();
                var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString ?? "{}") ?? new Dictionary<string, string>();
            }
            catch
            {
                _cache = new Dictionary<string, string>();
            }
        }
    }
}