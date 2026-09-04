using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;

namespace Paymentcsharp441Lambda.Services;

public static class SecretsHelper
{
    private static readonly object _lock = new();
    private static Dictionary<string, string>? _cache;
    private static readonly ILogger _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("SecretsHelper");

    public static string Get(string secretKey, string envFallback)
    {
        try
        {
            if (_cache == null)
            {
                lock (_lock)
                {
                    if (_cache == null)
                    {
                        var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
                        if (string.IsNullOrWhiteSpace(secretName))
                        {
                            _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }
                        else
                        {
                            var client = new AmazonSecretsManagerClient();
                            var resp = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
                            _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString ?? "{}") ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            if (_cache != null && _cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Secrets lookup failed for key {SecretKey}: {Message}", secretKey, ex.Message);
        }
        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
}