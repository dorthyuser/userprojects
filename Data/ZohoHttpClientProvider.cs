using System;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace hello_http_test.Data
{
    public class ZohoHttpClientProvider
    {
        private readonly IHttpClientFactory _factory;
        private readonly SecretClient _secretClient;
        private readonly ILogger<ZohoHttpClientProvider> _logger;

        public ZohoHttpClientProvider(IHttpClientFactory factory, SecretClient secretClient, ILogger<ZohoHttpClientProvider> logger)
        {
            _factory = factory;
            _secretClient = secretClient;
            _logger = logger;
        }

        public HttpClient CreateClient(string clientName)
        {
            var client = _factory.CreateClient(clientName);
            try
            {
                var baseUrl = _secretClient.GetSecret("ZOHO_store_api_url").Value?.Value;
                if (!string.IsNullOrEmpty(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
                {
                    client.BaseAddress = uri;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to set base address from Key Vault for Zoho client");
            }

            return client;
        }
    }
}
