using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace new_project.Services;

public static class SecretHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private static readonly SecretClient? Client;
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { });
    private static readonly ILogger Logger = LoggerFactory.CreateLogger("SecretHelper");

    static SecretHelper()
    {
        var keyVaultUri = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            try
            {
                Client = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize Key Vault client");
                Client = null;
            }
        }
    }

    public static string Get(string secretName, string envFallback)
    {
        if (Cache.TryGetValue(secretName, out var cached))
        {
            return cached;
        }

        if (Client is not null)
        {
            try
            {
                var value = Client.GetSecret(secretName).Value.Value ?? string.Empty;
                Cache[secretName] = value;
                return value;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read secret {SecretName} from Key Vault", secretName);
            }
        }

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
}