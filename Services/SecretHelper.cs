using System;
using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace TravelcardDb.Services;

public static class SecretHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private static readonly object SyncRoot = new();
    private static readonly ILogger Logger;
    private static readonly SecretClient? Client;

    static SecretHelper()
    {
        Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("SecretHelper");
        var keyVaultUrl = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URL");
        if (string.IsNullOrWhiteSpace(keyVaultUrl))
        {
            Logger.LogWarning("[SecretHelper] Key Vault URL not configured — using env vars as fallback");
            Client = null;
            return;
        }

        try
        {
            Client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[SecretHelper] Key Vault init failed: {Reason}", ex.Message);
            Client = null;
        }
    }

    public static string Get(string secretName, string envFallback)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            Logger.LogWarning("[SecretHelper] Secret name is empty; using env fallback '{Fallback}'", envFallback);
            return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
        }

        if (Cache.TryGetValue(secretName, out var cached))
        {
            return cached;
        }

        if (Client != null)
        {
            lock (SyncRoot)
            {
                if (Cache.TryGetValue(secretName, out cached))
                {
                    return cached;
                }

                try
                {
                    var secret = Client.GetSecret(secretName);
                    if (!string.IsNullOrWhiteSpace(secret.Value.Value))
                    {
                        Cache[secretName] = secret.Value.Value;
                        return secret.Value.Value;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "[SecretHelper] KV lookup failed for '{SecretName}': {Reason} — using env fallback", secretName, ex.Message);
                }
            }
        }

        var fallback = Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fallback))
        {
            Logger.LogWarning("[SecretHelper] WARNING: '{SecretName}' resolved to empty", secretName);
        }

        return fallback;
    }
}