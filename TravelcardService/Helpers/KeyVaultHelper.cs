using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;

namespace TravelcardService.Helpers
{
    public class KeyVaultHelper : IKeyVaultHelper
    {
        private readonly SecretClient _secretClient;
        private readonly ConcurrentDictionary<string, string> _cache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public KeyVaultHelper(SecretClient secretClient)
        {
            _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
        }

        public async Task<string?> GetSecretAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Secret name must be provided", nameof(name));
            }

            if (_cache.TryGetValue(name, out var cached))
            {
                return cached;
            }

            var secret = await _secretClient.GetSecretAsync(name).ConfigureAwait(false);
            string value = secret.Value.Value ?? string.Empty;
            _cache.TryAdd(name, value);
            return value;
        }
    }
}
