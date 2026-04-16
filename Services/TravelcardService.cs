using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TcTestingZoho.Services
{
    public class TravelcardService : ITravelcardService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenService _tokenService;
        private readonly ILogger<TravelcardService> _logger;
        private readonly string _baseUrl;
        private readonly string _functionKey;

        public TravelcardService(IHttpClientFactory httpClientFactory, ITokenService tokenService, IConfiguration configuration, ILogger<TravelcardService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _tokenService = tokenService;
            _logger = logger;

            _baseUrl = Environment.GetEnvironmentVariable("TRAVELCARD_API_URL") ?? configuration["Travelcard:ApiUrl"] ?? string.Empty;
            _functionKey = Environment.GetEnvironmentVariable("TRAVELCARD_FUNCTION_KEY") ?? configuration["Travelcard:FunctionKey"] ?? string.Empty;

            if (string.IsNullOrEmpty(_baseUrl))
            {
                _logger.LogWarning("TRAVELCARD_API_URL is not set in environment or configuration.");
            }

            if (string.IsNullOrEmpty(_functionKey))
            {
                _logger.LogWarning("TRAVELCARD_FUNCTION_KEY is not set in environment or configuration.");
            }
        }

        public async Task<ForwardResult> ForwardAsync(string body)
        {
            if (string.IsNullOrEmpty(_baseUrl))
            {
                return new ForwardResult { IsSuccess = false, StatusCode = 502, Error = "Travelcard API URL is not configured." };
            }

            if (string.IsNullOrEmpty(_functionKey))
            {
                return new ForwardResult { IsSuccess = false, StatusCode = 502, Error = "Travelcard function key is not configured." };
            }

            var accessToken = await _tokenService.GetAccessTokenAsync();
            var clientIdHeader = _tokenService.GetClientId() ?? string.Empty;

            try
            {
                var client = _httpClientFactory.CreateClient("TravelcardClient");

                var requestUri = _baseUrl;
                // append function key as query parameter
                var separator = requestUri.Contains("?") ? "&" : "?";
                requestUri = requestUri + separator + "code=" + Uri.EscapeDataString(_functionKey);

                using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new StringContent(body ?? string.Empty, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrEmpty(clientIdHeader))
                {
                    request.Headers.Add("client_id", clientIdHeader);
                }

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ForwardResult { IsSuccess = true, StatusCode = (int)response.StatusCode, Content = content };
                }

                _logger.LogWarning("Upstream travelcard API returned status {status}. Response: {response}", response.StatusCode, content);
                return new ForwardResult { IsSuccess = false, StatusCode = (int)response.StatusCode, Error = content };
            }
            catch (TokenException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while forwarding request to travelcard API.");
                return new ForwardResult { IsSuccess = false, StatusCode = 502, Error = "Error while forwarding request to travelcard API." };
            }
        }
    }
}
