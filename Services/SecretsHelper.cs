using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace Paymentcsharp441Lambda.Services;

public static class SecretsHelper
{
    private static readonly object Sync = new();
    private static Dictionary<string, string>? _cache;

    public static string Get(string secretKey, string envFallback)
    {
        if (_cache == null)
        {
            lock (Sync)
            {
                if (_cache == null)
                {
                    try
                    {
                        var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
                        if (string.IsNullOrWhiteSpace(secretName))
                        {
                            _cache = new Dictionary<string, string>();
                        }
                        else
                        {
                            using var client = new AmazonSecretsManagerClient();
                            var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                            _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
                        }
                    }
                    catch
                    {
                        _cache = new Dictionary<string, string>();
                    }
                }
            }
        }
        if (_cache != null && _cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
}
