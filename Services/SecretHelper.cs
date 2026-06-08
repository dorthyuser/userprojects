using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Concurrent;

namespace LifeTimeCalculator.Services;

public static class SecretHelper
{
    private static readonly SecretClient? _client;
    private static readonly ConcurrentDictionary<string, string> _cache = new();

    static SecretHelper()
    {
        var uri = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");
        if (!string.IsNullOrEmpty(uri))
        {
            try
            {
                _client = new SecretClient(new Uri(uri), new DefaultAzureCredential());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SecretHelper] Key Vault init failed: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("[SecretHelper] AZURE_KEY_VAULT_URI not set — using env vars as primary source");
        }
    }

    public static string Get(string secretName, string envFallback)
    {
        if (_cache.TryGetValue(secretName, out var cached)) return cached;

        if (_client != null)
        {
            try
            {
                var value = _client.GetSecret(secretName).Value.Value;
                _cache[secretName] = value;
                Console.WriteLine($"[SecretHelper] Secret '{secretName}' resolved from Key Vault");
                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SecretHelper] KV secret '{secretName}' not found — falling back to env var '{envFallback}': {ex.Message}");
            }
        }

        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
}
