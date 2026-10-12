using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace HotelBookingService.Infrastructure;

public static class SecretsHelper
{
    private static readonly object SyncRoot = new();
    private static Dictionary<string, string>? _cache;

    public static string Get(string secretKey, string envFallback)
    {
        EnsureLoaded();
        if (_cache is not null && _cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }

    private static void EnsureLoaded()
    {
        if (_cache is not null)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_cache is not null)
            {
                return;
            }

            try
            {
                var secretName = Environment.GetEnvironmentVariable("SECRET_NAME");
                if (string.IsNullOrWhiteSpace(secretName))
                {
                    _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    return;
                }

                using var client = new AmazonSecretsManagerClient();
                var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString ?? "{}") ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
