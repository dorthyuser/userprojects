using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Csharpae1140Lambda.Services;

public static class SecretsHelper
{
    private static readonly object Sync = new();
    private static Dictionary<string, string>? _cache;
    private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("SecretsHelper");

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            EnsureCache();
            if (_cache is not null && _cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        }
        catch
        {
        }
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
                var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
                Logger.LogInformation("Loaded secret keys: {Keys}", string.Join(",", _cache.Keys));
            }
            catch
            {
                _cache = new Dictionary<string, string>();
            }
        }
    }
}