using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TravelcardGatewayService.Models;

namespace TravelcardGatewayService.Helpers
{
    public class TravelcardHttpClient : ITravelcardHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly TokenService _tokenService;
        private readonly ILogger<TravelcardHttpClient> _logger;
        private readonly string _clientIdHeader;

        public TravelcardHttpClient(HttpClient httpClient, TokenService tokenService, ILogger<TravelcardHttpClient> logger)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
            _logger = logger;
            _clientIdHeader = Environment.GetEnvironmentVariable("TRAVELCARD_CLIENT_ID") ?? string.Empty;
        }
        public async Task<TravelcardResponse> ForwardRawAsync(string jsonBody)
        {
            _logger.LogInformation("Forwarding raw payload to backend");

            if (string.IsNullOrWhiteSpace(_clientIdHeader))
            {
                _logger.LogError("TRAVELCARD_CLIENT_ID environment variable is missing");
                throw new BackendException("Missing TRAVELCARD_CLIENT_ID");
            }
            var token = await _tokenService.GetAccessTokenAsync();

            var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/travelcard")
            {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            httpReq.Headers.Add("client_id", _clientIdHeader);
            httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage resp;
            try
            {
                resp = await _httpClient.SendAsync(httpReq);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP call to backend failed");
                throw new BackendException("Failed to call backend API");
            }

            var respBody = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Backend returned error {Status}: {Body}", resp.StatusCode, respBody);
                throw new BackendException($"Backend error: {resp.StatusCode}, Body: {respBody}");
            }
            return JsonSerializer.Deserialize<TravelcardResponse>(
            respBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            )!;
        }

        public async Task<TravelcardResponse> ForwardTravelcardAsync(TravelcardRequest request)
        {
            _logger.LogInformation("Enter TravelcardHttpClient.ForwardTravelcardAsync");

            if (string.IsNullOrWhiteSpace(_clientIdHeader))
            {
                _logger.LogError("TRAVELCARD_CLIENT_ID environment variable is missing");
                throw new BackendException("Missing TRAVELCARD_CLIENT_ID");
            }

            var token = await _tokenService.GetAccessTokenAsync();

            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/travelcard")
            {
                Content = content
            };

            httpReq.Headers.Add("client_id", _clientIdHeader);
            httpReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage resp;
            try
            {
                resp = await _httpClient.SendAsync(httpReq);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP call to backend failed");
                throw new BackendException("Failed to call backend API");
            }

            var respBody = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Backend returned error {Status}: {Body}", resp.StatusCode, respBody);
                throw new BackendException($"Backend error: {resp.StatusCode}");
            }

            try
            {
                var backendResponse = JsonSerializer.Deserialize<TravelcardResponse>(respBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (backendResponse == null)
                {
                    _logger.LogError("Unable to deserialize backend response");
                    throw new BackendException("Invalid backend response");
                }

                _logger.LogInformation("Exit TravelcardHttpClient.ForwardTravelcardAsync");
                return backendResponse;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse backend response JSON");
                throw new BackendException("Invalid JSON from backend");
            }
        }
    }
}
