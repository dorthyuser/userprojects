using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace currency_calculator.Infrastructure;

public static class SecretHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private static readonly SecretClient? Client;

    static SecretHelper()
    {
        try
        {
            var vaultUri = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");
            if (!string.IsNullOrWhiteSpace(vaultUri))
            {
                Client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
            }
        }
        catch
        {
            Client = null;
        }
    }

    public static string Get(string secretName, string envFallback)
    {
        if (Cache.TryGetValue(secretName, out var cached))
        {
            return cached;
        }

        try
        {
            if (Client is not null)
            {
                var value = Client.GetSecret(secretName).Value.Value ?? string.Empty;
                Cache[secretName] = value;
                return value;
            }
        }
        catch
        {
        }

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
}