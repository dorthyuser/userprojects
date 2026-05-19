using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Concurrent;

namespace TravelCardFunctionApp.Services;

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
            catch (Exception)
            {
            }
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
                return value;
            }
            catch (Exception)
            {
            }
        }
        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
}
