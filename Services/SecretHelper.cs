using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace synctesting1109.Services;

public static class SecretHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private static readonly SecretClient? Client;

    static SecretHelper()
    {
        try
        {
            var keyVaultUrl = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");
            if (string.IsNullOrWhiteSpace(keyVaultUrl))
            {
                Console.WriteLine("[SecretHelper] Key Vault URL not configured — using env vars as fallback");
                Client = null;
                return;
            }

            Client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SecretHelper] Key Vault init failed: {ex.Message}");
            Client = null;
        }
    }

    public static string Get(string secretName, string envFallback)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            Console.WriteLine($"[SecretHelper] WARNING: secret name empty, reading env fallback '{envFallback}' directly");
            return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
        }

        if (Cache.TryGetValue(secretName, out var cached))
        {
            return cached;
        }

        if (Client != null)
        {
            try
            {
                var secret = Client.GetSecret(secretName);
                var value = secret.Value.Value;
                Cache[secretName] = value;
                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SecretHelper] KV lookup failed for '{secretName}': {ex.Message} — using env fallback");
            }
        }

        var envValue = Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(envValue))
        {
            Console.WriteLine($"[SecretHelper] WARNING: '{secretName}' resolved to empty");
        }

        return envValue;
    }
}