using System;
using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace jkjkjkjkjkjkjkjkjkjkjkjkjkjjk.Services
{
    public static class SecretHelper
    {
        private static readonly ConcurrentDictionary<string, string> Cache = new();
        private static readonly SecretClient? Client;
        private static readonly object Sync = new();
        private static ILogger? _logger;

        static SecretHelper()
        {
            try
            {
                var vaultUri = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");
                if (!string.IsNullOrWhiteSpace(vaultUri))
                {
                    Client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
                }
            }
            catch
            {
                Client = null;
            }
        }

        public static void SetLogger(ILogger logger)
        {
            lock (Sync)
            {
                _logger ??= logger;
            }
        }

        public static string Get(string secretName, string envFallback)
        {
            if (Cache.TryGetValue(secretName, out var cached))
            {
                return cached;
            }

            try
            {
                if (Client != null)
                {
                    var value = Client.GetSecret(secretName).Value.Value ?? string.Empty;
                    Cache[secretName] = value;
                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Secret resolution failed for {SecretName}", secretName);
            }

            return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
        }
    }
}