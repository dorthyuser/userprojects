using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Collections.Concurrent;

namespace azurecsharpfunction534.Services;

public static class SecretHelper
{
    private static readonly SecretClient? _client;
    private static readonly ConcurrentDictionary<string, string> _cache = new();

    static SecretHelper()
    {
        var uri = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");
        if (!string.IsNullOrEmpty(uri))
        {
            try { _client = new SecretClient(new Uri(uri), new DefaultAzureCredential()); }
            catch (Exception ex) { Console.WriteLine($"[SecretHelper] KV init failed: {ex.Message}"); }
        }
        else { Console.WriteLine("[SecretHelper] AZURE_KEY_VAULT_URI not set — env vars used as primary"); }
    }

    public static string Get(string secretName, string envFallback)
    {
        if (_cache.TryGetValue(secretName, out var cached)) return cached;
        if (_client != null)
        {
            try
            {
                var val = _client.GetSecret(secretName).Value.Value;
                _cache[secretName] = val;
                return val;
            }
            catch (Exception ex) { Console.WriteLine($"[SecretHelper] '{secretName}' not in KV, using env '{envFallback}': {ex.Message}"); }
        }
        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
}