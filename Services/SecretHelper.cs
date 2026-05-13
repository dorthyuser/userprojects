using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace test_capi_1111123323333.Services;

public static class SecretHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private static readonly ILogger Logger;
    private static readonly SecretClient? Client;

    static SecretHelper()
    {
        Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("SecretHelper");
        var keyVaultUri = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");
        if (!string.IsNullOrWhiteSpace(keyVaultUri) && Uri.TryCreate(keyVaultUri, UriKind.Absolute, out var uri))
        {
            try
            {
                Client = new SecretClient(uri, new DefaultAzureCredential());
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Secret client initialization failed");
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
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Secret retrieval failed for {SecretName}", secretName);
            }
        }
        var fallback = Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
        if (!string.IsNullOrEmpty(fallback)) Cache[secretName] = fallback;
        return fallback;
    }
}