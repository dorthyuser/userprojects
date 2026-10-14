using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace hello.Infrastructure;

public interface ISecretProvider
{
    Task<DatabaseSecret> GetDatabaseSecretAsync(CancellationToken cancellationToken);
}

public sealed class AwsSecretsManagerSecretProvider : ISecretProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AwsSecretsManagerSecretProvider> _logger;

    public AwsSecretsManagerSecretProvider(IConfiguration configuration, ILogger<AwsSecretsManagerSecretProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DatabaseSecret> GetDatabaseSecretAsync(CancellationToken cancellationToken)
    {
        var secretName = _configuration["POSTGRESQL_SECRET"] ?? _configuration["AWS_SECRET_NAME"];
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new InvalidOperationException("Database secret name is not configured.");
        }

        var region = _configuration["AWS_REGION"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-2";
        using var client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(region));
        var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }, cancellationToken);
        if (string.IsNullOrWhiteSpace(response.SecretString))
        {
            throw new InvalidOperationException("Secret payload is empty.");
        }

        var secret = JsonSerializer.Deserialize<DatabaseSecret>(response.SecretString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return secret ?? throw new InvalidOperationException("Secret payload is invalid.");
    }
}

public sealed record DatabaseSecret(string Host, int Port, string DbName, string Username, string Password);
