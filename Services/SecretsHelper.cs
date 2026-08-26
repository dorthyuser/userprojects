using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace Paymentcsharp441Lambda.Services;

public static class SecretsHelper
{
    private static readonly object Sync = new();
    private static Dictionary<string, string>? _cache;

    /// <summary>
    /// Returns the value for <paramref name="secretKey"/> from the AWS Secrets Manager secret
    /// named by <c>AWS_SECRET_NAME</c>, falling back to the environment variable
    /// <paramref name="envFallback"/> if the secret is unavailable or the key is absent.
    /// </summary>
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
                            var resp = client
                                .GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName })
                                .GetAwaiter()
                                .GetResult();
                            _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(
                                         resp.SecretString ?? "{}")
                                     ?? new Dictionary<string, string>();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the real reason secrets failed to load so it appears in CloudWatch
                        // rather than silently falling back to empty strings.
                        Console.Error.WriteLine(
                            $"SecretsHelper: failed to load secret " +
                            $"'{Environment.GetEnvironmentVariable("AWS_SECRET_NAME")}': {ex}");
                        _cache = new Dictionary<string, string>();
                    }
                }
            }
        }

        if (_cache != null && _cache.TryGetValue(secretKey, out var value) && !string.IsNullOrWhiteSpace(value))
            return value;

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
}
