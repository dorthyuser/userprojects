using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace Travelcardlambdacsharp1105Lambda.Services;

public static class SecretsHelper
{
    private static Dictionary<string, string>? _cache;
    private static readonly object Sync = new();

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            var cache = _cache;
            if (cache is null)
            {
                lock (Sync)
                {
                    cache ??= _cache;
                    if (cache is null)
                    {
                        var secretId = Environment.GetEnvironmentVariable("AWS_SECRET");
                        if (!string.IsNullOrWhiteSpace(secretId))
                        {
                            using var client = new AmazonSecretsManagerClient();
                            var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId }).GetAwaiter().GetResult();
                            cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
                            _cache = cache;
                        }
                    }
                }
            }
            if (cache != null && cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        }
        catch
        {
        }
        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
}