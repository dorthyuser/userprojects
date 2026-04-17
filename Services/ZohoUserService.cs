using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using ZohoCrmOauthFinal.Models;

namespace ZohoCrmOauthFinal.Services
{
    public class ZohoUserService : IZohoUserService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SecretClient _secretClient;
        private readonly ITokenService _tokenService;
        private readonly ILogger<ZohoUserService> _logger;

        public ZohoUserService(IHttpClientFactory httpClientFactory, SecretClient secretClient, ITokenService tokenService, ILogger<ZohoUserService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ZohoUsersResponse?> GetUsersAsync()
        {
            var http = _httpClientFactory.CreateClient("zoho-crm-connection");

            // Resolve base URL from Key Vault (TWO-STEP)
            var baseUrlKey = Environment.GetEnvironmentVariable("ZOHO_BASE_URL");
            if (string.IsNullOrEmpty(baseUrlKey))
                throw new InvalidOperationException("Env var 'ZOHO_BASE_URL' is not set");

            var baseUrl = (await _secretClient.GetSecretAsync(baseUrlKey)).Value.Value;
            if (string.IsNullOrEmpty(baseUrl))
                throw new InvalidOperationException("ZOHO_BASE_URL key resolved to empty value in Key Vault");

            try
            {
                http.BaseAddress = new Uri(baseUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid base URL for Zoho CRM: {BaseUrl}", baseUrl);
                throw;
            }

            var token = await _tokenService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("Unable to obtain access token");

            var request = new HttpRequestMessage(HttpMethod.Get, "/crm/v2/users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage resp;
            try
            {
                resp = await http.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request to Zoho CRM failed");
                throw;
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogError("Zoho CRM returned non-success: {Status} {Body}", resp.StatusCode, body);
                throw new InvalidOperationException("Zoho CRM returned an error");
            }

            var content = await resp.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<ZohoUsersResponse>(content);
            return users;
        }
    }
}
