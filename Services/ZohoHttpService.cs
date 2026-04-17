using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZohoOauthKvTest.Services
{
    public class ZohoHttpService : IZohoHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenService _tokenService;
        private readonly ILogger<ZohoHttpService> _logger;

        public ZohoHttpService(IHttpClientFactory httpClientFactory, ITokenService tokenService, ILogger<ZohoHttpService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> GetLeadsAsync()
        {
            var client = _httpClientFactory.CreateClient("Zoho");
            try
            {
                var token = await _token_service.GetAccessTokenAsync();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await client.GetAsync("crm/v2/Leads");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Zoho API returned non-success status {Status}", response.StatusCode);
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Zoho API");
                throw;
            }
        }
    }
}
