using AdverseEventNotificationService.Api.Configuration;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace AdverseEventNotificationService.Api.Services;

public sealed class AzureKeyVaultSecretProvider : ISecretProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureKeyVaultSecretProvider> _logger;

    public AzureKeyVaultSecretProvider(IConfiguration configuration, ILogger<AzureKeyVaultSecretProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DatabaseConnectionSettings?> GetDatabaseConnectionSettingsAsync(CancellationToken cancellationToken)
    {
        var vaultUri = _configuration["AZURE_KEY_VAULT_URI"];
        if (string.IsNullOrWhiteSpace(vaultUri))
        {
            return null;
        }

        try
        {
            var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
            var host = (await client.GetSecretAsync(SecretNames.PostgresHost, cancellationToken: cancellationToken)).Value.Value;
            var portText = (await client.GetSecretAsync(SecretNames.PostgresPort, cancellationToken: cancellationToken)).Value.Value;
            var database = (await client.GetSecretAsync(SecretNames.PostgresDatabase, cancellationToken: cancellationToken)).Value.Value;
            var username = (await client.GetSecretAsync(SecretNames.PostgresUsername, cancellationToken: cancellationToken)).Value.Value;
            var password = (await client.GetSecretAsync(SecretNames.PostgresPassword, cancellationToken: cancellationToken)).Value.Value;

            return new DatabaseConnectionSettings(host, int.Parse(portText), database, username, password);
        }
        catch (Exception)
        {
            _logger.LogError("Key Vault secret retrieval failed");
            return null;
        }
    }
}