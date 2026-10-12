using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Options;
using PrimeNumberService.Configuration;

namespace PrimeNumberService.Security;

public sealed class AwsSecretsManagerSecretProvider : ISecretProvider
{
    private readonly IAmazonSecretsManager _client;
    private readonly ServiceOptions _options;
    private readonly ILogger<AwsSecretsManagerSecretProvider> _logger;

    public AwsSecretsManagerSecretProvider(IOptions<ServiceOptions> options, ILogger<AwsSecretsManagerSecretProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new AmazonSecretsManagerClient();
    }

    public async Task<DatabaseSecret> GetDatabaseSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        var resolvedSecretName = string.IsNullOrWhiteSpace(secretName) ? _options.SecretsManagerSecretName : secretName;
        if (string.IsNullOrWhiteSpace(resolvedSecretName))
        {
            throw new InvalidOperationException("Secrets Manager secret name is not configured");
        }

        var response = await _client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = resolvedSecretName }, cancellationToken);
        if (string.IsNullOrWhiteSpace(response.SecretString))
        {
            throw new InvalidOperationException("Secret payload is empty");
        }

        using var document = JsonDocument.Parse(response.SecretString);
        var root = document.RootElement;
        return new DatabaseSecret(
            root.GetProperty("host").GetString() ?? throw new InvalidOperationException("Secret missing host"),
            root.TryGetProperty("port", out var port) ? port.GetInt32() : 5432,
            root.GetProperty("dbname").GetString() ?? throw new InvalidOperationException("Secret missing dbname"),
            root.GetProperty("username").GetString() ?? throw new InvalidOperationException("Secret missing username"),
            root.GetProperty("password").GetString() ?? throw new InvalidOperationException("Secret missing password"));
    }
}