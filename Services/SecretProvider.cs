using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace tc_csharp_api
{
    /// <summary>
    /// A minimal secret provider that resolves secret entries from IConfiguration or environment variables.
    /// This avoids hardcoding secrets in source. In real deployments, implement a connector to the platform secret store.
    /// </summary>
    public class SecretProvider : ISecretProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecretProvider> _logger;

        public SecretProvider(IConfiguration configuration, ILogger<SecretProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task<IDictionary<string, string>> GetSecretAsync(string secretName)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Try to resolve keys by prefix: {secretName}:KEY
                var keys = new[] { "AZURE-CLIENT-ID", "AZURE-CLIENT-SECRET", "AZURE-TOKEN-URL", "AZURE-SCOPES" };
                foreach (var key in keys)
                {
                    var val = _configuration[$"{secretName}:{key}"] ?? Environment.GetEnvironmentVariable(key) ?? _configuration[key];
                    if (!string.IsNullOrEmpty(val))
                    {
                        result[key] = val;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read secret {SecretName} from configuration", secretName);
            }

            return Task.FromResult((IDictionary<string, string>)result);
        }
    }
}
