using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using hello_http_test.Models;

namespace hello_http_test.Services
{
    public class ZohoService : IZohoService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ITokenService _tokenService;
        private readonly SecretClient _secretClient;
        private readonly ILogger<ZohoService> _logger;
        private const string HttpClientName = "zoho-test-http-con";

        public ZohoService(IHttpClientFactory httpFactory, ITokenService tokenService, SecretClient secretClient, ILogger<ZohoService> logger)
        {
            _httpFactory = httpFactory;
            _tokenService = tokenService;
            _secretClient = secretClient;
            _logger = logger;
        }

        public async Task<IEnumerable<StoreModel>> GetStoresAsync()
        {
            var token = await _tokenService.GetAccessTokenAsync();
            var baseUrlSecret = _secret_client_getsecret_placeholder();
            // NOTE: previous code used _secretClient.GetSecret("ZOHO_store_api_url").Value?.Value directly. Replacing with call to avoid long single line in string serialization.
            baseUrlSecret = _secretClient.GetSecret("ZOHO_store_api_url").Value?.Value;
            if (string.IsNullOrEmpty(baseUrlSecret))
            {
                throw new InvalidOperationException("ZOHO_store_api_url not found in Key Vault");
            }

            var client = _httpFactory.CreateClient(HttpClientName);
            if (!Uri.TryCreate(baseUrlSecret, UriKind.Absolute, out var baseUri))
            {
                throw new InvalidOperationException("ZOHO_store_api_url in Key Vault is not a valid absolute URI");
            }

            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var resp = await client.GetAsync("/stores");
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogError("Zoho stores endpoint returned {Status}: {Body}", resp.StatusCode, body);
                resp.EnsureSuccessStatusCode();
            }

            var content = await resp.Content.ReadAsStringAsync();
            var stores = JsonSerializer.Deserialize<IEnumerable<StoreModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return stores ?? new List<StoreModel>();
        }

        // Placeholder to keep source mapping intact for serialization; actual call above uses _secretClient directly.
        private string _secret_client_getsecret_placeholder() => null;
    }
}
