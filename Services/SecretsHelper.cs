using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LambdacsharphttpLambda.Services;

public sealed class SecretsHelper
{
    private readonly ILogger<SecretsHelper> _logger;
    private readonly IAmazonSecretsManager _secretsManager;
    private Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public SecretsHelper(ILogger<SecretsHelper> logger)
    {
        _logger = logger;
        _secretsManager = new AmazonSecretsManagerClient();
    }

    public async Task<Dictionary<string, string>> GetSecretsAsync(string secretName)
    {
        try
        {
            if (_cache.Count > 0)
            {
                return _cache;
            }

            var response = await _secretsManager.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName });
            var secretJson = response.SecretString ?? "{}";
            var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(secretJson) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _cache = new Dictionary<string, string>(secrets, StringComparer.OrdinalIgnoreCase);
            return _cache;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secrets for secret name {SecretName}", secretName);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public string Resolve(Dictionary<string, string> secrets, string key)
    {
        if (secrets.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        throw new InvalidOperationException($"{key} not configured");
    }
}