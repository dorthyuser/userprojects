using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace tc_csharp_api
{
    public class TravelcardService : ITravelcardService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TravelcardService> _logger;

        public TravelcardService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TravelcardService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> ForwardAsync(string rawBody)
        {
            var client = _httpClientFactory.CreateClient("sb-connection");

            var travelcardBase = Environment.GetEnvironmentVariable("TRAVELCARD_API_URL") ?? _configuration["TRAVELCARD_API_URL"];
            var functionKey = Environment.GetEnvironmentVariable("TRAVELCARD_FUNCTION_KEY") ?? _configuration["TRAVELCARD_FUNCTION_KEY"];

            if (string.IsNullOrEmpty(travelcardBase) || string.IsNullOrEmpty(functionKey))
            {
                throw new ExternalApiException("Travelcard API configuration missing", System.Net.HttpStatusCode.BadGateway);
            }

            var url = travelcardBase.Contains("?") ? $"{travelcardBase}&code={functionKey}" : $"{travelcardBase}?code={functionKey}";

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(rawBody ?? string.Empty, Encoding.UTF8, "application/json")
            };

            // Mandatory outgoing header: client_id
            var clientId = Environment.GetEnvironmentVariable("AZURE-CLIENT-ID") ?? _configuration["AZURE-CLIENT-ID"];
            if (!string.IsNullOrEmpty(clientId))
            {
                if (!request.Headers.Contains("client_id"))
                {
                    request.Headers.Add("client_id", clientId);
                }
            }

            try
            {
                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("External API returned non-success status {Status} with content: {Content}", response.StatusCode, content);
                    throw new ExternalApiException(content, response.StatusCode);
                }

                return response;
            }
            catch (ExternalApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling external travelcard API");
                throw new ExternalApiException("Error calling external Travelcard API: " + ex.Message, System.Net.HttpStatusCode.BadGateway);
            }
        }
    }
}
