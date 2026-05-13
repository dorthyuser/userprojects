using System;
using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace httptestingapi.Services;

public static class SecretHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly ILogger Logger = LoggerFactory.CreateLogger("SecretHelper");
    private static readonly SecretClient? Client;

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
                Logger.LogError(ex, "Secret client initialization failed");
                Client = null;
            }
        }
    }

    public static string Get(string secretName, string envFallback)
    {
        if (Cache.TryGetValue(secretName, out var cached)) return cached;
        if (Client != null)
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
