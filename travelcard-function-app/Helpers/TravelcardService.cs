using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers
{
    public class TravelcardService : ITravelcardService
    {
        private readonly IHttpHelper _httpHelper;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public TravelcardService(IHttpHelper httpHelper, HttpClient httpClient, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _httpHelper = httpHelper;
            _httpClient = httpClient;
            _logger = loggerFactory.CreateLogger<TravelcardService>();
            _configuration = configuration;
        }

        public async Task<TravelcardResponse> PostTravelcardAsync(TravelcardRequest request)
        {
            _logger.LogInformation("Entering TravelcardService.PostTravelcardAsync at {Time}", DateTime.UtcNow);
            try
            {
                // Determine token parameters: prefer values from request, fallback to environment configuration
                var clientId = request.Auth!.OAuth2!.ClientId ?? _configuration["AZURE_CLIENT_ID"];
                var clientSecret = request.Auth.OAuth2.ClientSecret ?? _configuration["AZURE_CLIENT_SECRET"];
                var tokenUrl = request.Auth.OAuth2.TokenUrl ?? _configuration["AZURE_TOKEN_URL"];
                var scopesArray = request.Auth.OAuth2.Scopes ?? Array.Empty<string>();
                var scopes = scopesArray.Length > 0 ? string.Join(' ', scopesArray) : _configuration["AZURE_SCOPES"];

                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(tokenUrl))
                {
                    var msg = "OAuth2 configuration is missing (clientId, clientSecret, or tokenUrl).";
                    _logger.LogError(msg);
                    return new TravelcardResponse { Success = false, StatusCode = 400, Data = new ErrorResponse { Error = new ErrorDetail { Message = "Configuration error", Detail = msg } } };
                }

                var token = await _httpHelper.GetAccessTokenAsync(tokenUrl, clientId, clientSecret, scopes ?? string.Empty);

                var travelcardApiUrl = _configuration["TRAVELCARD_API_URL"];
                if (string.IsNullOrWhiteSpace(travelcardApiUrl))
                {
                    var msg = "TRAVELCARD_API_URL is not configured in environment variables.";
                    _logger.LogError(msg);
                    return new TravelcardResponse { Success = false, StatusCode = 500, Data = new ErrorResponse { Error = new ErrorDetail { Message = "Configuration error", Detail = msg } } };
                }

                var payload = JsonSerializer.Serialize(request);
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, travelcardApiUrl)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                _logger.LogInformation("Posting to Travelcard API at {Url}", travelcardApiUrl);
                using var resp = await _httpClient.SendAsync(httpRequest);
                var content = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Travelcard API returned {Status}: {Content}", resp.StatusCode, content);
                    return new TravelcardResponse { Success = false, StatusCode = (int)resp.StatusCode, Data = new ErrorResponse { Error = new ErrorDetail { Message = "Travelcard API error", Detail = content } } };
                }

                object? data = null;
                try
                {
                    data = JsonSerializer.Deserialize<JsonElement>(content);
                }
                catch
                {
                    data = content;
                }

                _logger.LogInformation("Travelcard API call succeeded with status {Status}", resp.StatusCode);
                _logger.LogInformation("Exiting TravelcardService.PostTravelcardAsync at {Time}", DateTime.UtcNow);

                return new TravelcardResponse { Success = true, StatusCode = (int)resp.StatusCode, Data = data };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting to Travelcard API");
                return new TravelcardResponse { Success = false, StatusCode = 500, Data = new ErrorResponse { Error = new ErrorDetail { Message = "Internal error", Detail = ex.Message } } };
            }
        }
    }
}
