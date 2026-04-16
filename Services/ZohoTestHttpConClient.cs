using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using hello_http_test.Models;

namespace hello_http_test.Services
{
    /// <summary>
    /// Dedicated HTTP client wrapper for the "zoho-test-http-con" connection.
    /// Exposes methods for Zoho Commerce endpoints and relies on a configured HttpClient
    /// that attaches OAuth2 bearer tokens via a DelegatingHandler.
    /// </summary>
    public class ZohoTestHttpConClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<ZohoTestHttpConClient> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Constructs the client with a configured HttpClient injected via IHttpClientFactory.
        /// </summary>
        public ZohoTestHttpConClient(HttpClient client, ILogger<ZohoTestHttpConClient> logger, IConfiguration configuration)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Retrieves stores from the Zoho API (GET /stores).
        /// </summary>
        public async Task<string> GetStoresAsync()
        {
            try
            {
                var resp = await _client.GetAsync("stores");
                resp.EnsureSuccessStatusCode();
                var body = await resp.Content.ReadAsStringAsync();
                return body;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed when calling GET /stores");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when calling GET /stores");
                throw;
            }
        }

        /// <summary>
        /// Creates a store via POST /stores.
        /// Accepts any object (Store or StoreDto) to allow callers to pass their DTOs directly.
        /// </summary>
        public async Task<string> CreateStoreAsync(object store)
        {
            try
            {
                var resp = await _client.PostAsJsonAsync("stores", store);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed when calling POST /stores");
                throw;
            }
        }

        /// <summary>
        /// Updates a store via PUT /stores/{id}.
        /// Accepts any object (Store or StoreDto) to allow callers to pass their DTOs directly.
        /// </summary>
        public async Task<string> UpdateStoreAsync(string id, object store)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            try
            {
                var resp = await _client.PutAsJsonAsync($"stores/{id}", store);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed when calling PUT /stores/{Id}", id);
                throw;
            }
        }
    }
}
