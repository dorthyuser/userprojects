using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using travelcard_service.Models;

namespace travelcard_service.Helpers
{
    public class HttpHelper
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpHelper> _logger;
        private readonly string _baseUrl;
        private readonly string _clientIdHeaderValue;

        public HttpHelper(HttpClient httpClient, ILogger<HttpHelper> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _baseUrl = Environment.GetEnvironmentVariable("TRAVELCARD_API_BASE_URL") ?? string.Empty;
            _clientIdHeaderValue = Environment.GetEnvironmentVariable("CLIENT_ID_HEADER") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                _logger.LogWarning("TRAVELCARD_API_BASE_URL is not set. Outgoing requests will fail.");
            }

            if (string.IsNullOrWhiteSpace(_clientIdHeaderValue))
            {
                _logger.LogWarning("CLIENT_ID_HEADER is not set. Outgoing requests may be rejected.");
            }
        }

        public async Task<BackendResponse> PostTravelcardAsync(TravelcardRequest request, string bearerToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_baseUrl))
                {
                    var msg = "TRAVELCARD_API_BASE_URL is not configured";
                    _logger.LogError(msg);
                    return new BackendResponse { IsSuccess = false, StatusCode = 500, Content = msg };
                }

                var functionKey = Environment.GetEnvironmentVariable("TRAVELCARD_FUNCTION_KEY") ?? string.Empty;
                var url = new Uri(new Uri(_baseUrl), $"api/travelcard?code={functionKey}");

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(request, options);
                _logger.LogInformation("Outgoing JSON Payload: {json}", json);
                using var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                // Required headers
                if (!string.IsNullOrWhiteSpace(_clientIdHeaderValue))
                {
                    httpReq.Headers.Add("client_id", _clientIdHeaderValue);
                }

                httpReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (!string.IsNullOrWhiteSpace(bearerToken))
                {
                    httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                }

                _logger.LogInformation("Sending request to backend {Url} with CardNumber {CardNumber}", url, request.CardNumber);

                var response = await _httpClient.SendAsync(httpReq);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Backend responded with {Status}. Content: {Content}", (int)response.StatusCode, content);
                    return new BackendResponse { IsSuccess = false, StatusCode = (int)response.StatusCode, Content = content };
                }

                _logger.LogInformation("Backend responded successfully with status {Status}", (int)response.StatusCode);
                return new BackendResponse { IsSuccess = true, StatusCode = (int)response.StatusCode, Content = content };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling backend API");
                return new BackendResponse { IsSuccess = false, StatusCode = 500, Content = ex.Message };
            }
        }
    }

    public class BackendResponse
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string? Content { get; set; }
    }
}
