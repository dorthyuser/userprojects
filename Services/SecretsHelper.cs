using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Httpcsharplambda.Services;

public class SecretsHelper
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
            var response = await _secretsManager.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName });
            var secretString = response.SecretString ?? "{}";
            _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(secretString) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return _cache;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Secret store lookup failed");
            _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return _cache;
        }
    }

    public async Task<string> ResolveValueAsync(Dictionary<string, string> secrets, string keyName)
    {
        if (secrets.TryGetValue(keyName, out var secretValue) && !string.IsNullOrWhiteSpace(secretValue))
        {
            return secretValue;
        }

        var envValue = Environment.GetEnvironmentVariable(keyName);
        return await Task.FromResult(envValue ?? throw new InvalidOperationException($"{keyName} is not configured"));
    }
}