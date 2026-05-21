using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace synctesting1050.Services;

public static class SecretHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private static readonly SecretClient? Client;

    static SecretHelper()
    {
        var vaultUrl = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");
        if (string.IsNullOrWhiteSpace(vaultUrl))
        {
            Console.WriteLine("[SecretHelper] Key Vault URL not configured — using env vars as fallback");
            return;
        }
        try
        {
            Client = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());
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
            Console.WriteLine($"[SecretHelper] WARNING: '{envFallback}' resolved to empty");
            return Environment.GetEnvironmentVariable(envFallback) ?? "";
        }
        if (Cache.TryGetValue(secretName, out var cached)) return cached;
        if (Client != null)
        {
            try
            {
                var secret = Client.GetSecret(secretName);
                var value = secret.Value.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Cache[secretName] = value;
                    return value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SecretHelper] KV lookup failed for '{secretName}': {ex.Message} — using env fallback");
            }
        }
        var env = Environment.GetEnvironmentVariable(envFallback) ?? "";
        if (string.IsNullOrWhiteSpace(env)) Console.WriteLine($"[SecretHelper] WARNING: '{secretName}' resolved to empty");
        return env;
    }
}