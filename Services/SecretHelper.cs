using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace zohotesting.Services;

public static class SecretHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private static readonly SecretClient? Client;
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly ILogger Logger = LoggerFactory.CreateLogger("SecretHelper");

    static SecretHelper()
    {
        var vaultUrl = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");
        if (string.IsNullOrWhiteSpace(vaultUrl))
        {
            Logger.LogWarning("[SecretHelper] Key Vault URL not configured — using env vars as fallback");
            return;
        }

        try
        {
            Client = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());
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
            Logger.LogWarning("[SecretHelper] Secret name empty, using env fallback");
            return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
        }

        if (Cache.TryGetValue(secretName, out var cached)) return cached;

        if (Client != null)
        {
            try
            {
                var value = Client.GetSecret(secretName).Value.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Cache[secretName] = value;
                    return value;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "[SecretHelper] KV lookup failed for '{SecretName}': {Reason} — using env fallback", secretName, ex.Message);
            }
        }

        var envValue = Environment.GetEnvironmentVariable(envFallback);
        if (string.IsNullOrWhiteSpace(envValue)) Logger.LogWarning("[SecretHelper] WARNING: '{SecretName}' resolved to empty", secretName);
        return envValue ?? string.Empty;
    }
}
