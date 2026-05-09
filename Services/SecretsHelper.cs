using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace TravelcardAppDemoLambda.Services;

public static class SecretsHelper
{
    private static readonly object LockObj = new();
    private static Dictionary<string, string>? _cache;

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            EnsureLoaded();
            if (_cache != null && _cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Secrets load error: {ex.Message}");
        }

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }

    private static void EnsureLoaded()
    {
        if (_cache != null) return;
        lock (LockObj)
        {
            if (_cache != null) return;
            try
            {
                var secretName = Environment.GetEnvironmentVariable("AWS_SECRET") ?? string.Empty;
                using var client = new AmazonSecretsManagerClient();
                var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Secrets helper exception: {ex.Message}");
                _cache = new Dictionary<string, string>();
            }
        }
    }
}