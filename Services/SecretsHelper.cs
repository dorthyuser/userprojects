using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace LambdatestingtravelcardLambda.Services;

public static class SecretsHelper
{
    private static readonly object Sync = new();
    private static Dictionary<string, string>? _cache;

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            EnsureLoaded();
            if (_cache is not null && _cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SecretsHelper error: {ex.Message}");
        }

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }

    private static void EnsureLoaded()
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
                var resp = Task.Run(async () =>
                {
                    using var client = new AmazonSecretsManagerClient();
                    return await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName });
                }).GetAwaiter().GetResult();

                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>();
                foreach (var key in _cache.Keys)
                {
                    Console.WriteLine($"Loaded secret key: {key}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load secrets: {ex.Message}");
                _cache = new Dictionary<string, string>();
            }
        }
    }
}