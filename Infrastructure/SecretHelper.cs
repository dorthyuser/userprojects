using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace csharpapi248pm.Infrastructure;

public static class SecretHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private static readonly SecretClient? Client;

    static SecretHelper()
    {
        var uri = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");
        if (!string.IsNullOrWhiteSpace(uri))
        {
            try
            {
                Client = new SecretClient(new Uri(uri), new DefaultAzureCredential());
            }
            catch
            {
                Client = null;
            }
        }
    }

    public static string Get(string secretName, string envFallback)
    {
        if (Cache.TryGetValue(secretName, out var cached)) return cached;
        if (Client is not null)
        {
            try
            {
                var value = Client.GetSecret(secretName).Value.Value ?? string.Empty;
                Cache[secretName] = value;
                return value;
            }
            catch
            {
            }
        }
        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
}