using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace TravelcardService.Helpers
{
    public interface IKeyVaultService
    {
        Task<string> GetSecretAsync(string secretName);
    }

    public class KeyVaultService : IKeyVaultService
    {
        private readonly SecretClient _client;
        private readonly ILogger _logger;

        public KeyVaultService(Microsoft.Extensions.Configuration.IConfiguration configuration, ILogger<KeyVaultService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var keyVaultUri = configuration["KeyVaultUri"];
            if (string.IsNullOrEmpty(keyVaultUri))
            {
                throw new ArgumentException("KeyVaultUri configuration is required");
            }

            _client = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            if (string.IsNullOrEmpty(secretName))
            {
                throw new ArgumentNullException(nameof(secretName));
            }

            try
            {
                var secret = await _client.GetSecretAsync(secretName);
                _logger.LogInformation("Retrieved secret {SecretName} from Key Vault", secretName);
                return secret.Value.Value ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret {SecretName} from Key Vault", secretName);
                throw;
            }
        }
    }
}
