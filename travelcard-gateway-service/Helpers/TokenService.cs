using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TravelcardGatewayService.Helpers
{
    public class TokenService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TokenService> _logger;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tokenUrl;
        private readonly string _scopes;
        private string? _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public TokenService(HttpClient httpClient, ILogger<TokenService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? string.Empty;
            _clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ?? string.Empty;
            _tokenUrl = Environment.GetEnvironmentVariable("AZURE_TOKEN_URL") ?? string.Empty;
            _scopes = Environment.GetEnvironmentVariable("AZURE_SCOPES") ?? string.Empty;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            _logger.LogInformation("Enter TokenService.GetAccessTokenAsync");

            if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiry > DateTime.UtcNow)
            {
                _logger.LogInformation("Using cached access token");
                return _cachedToken;
            }

            if (string.IsNullOrWhiteSpace(_tokenUrl) || string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret))
            {
                var msg = "Token service configuration missing environment variables";
                _logger.LogError(msg);
                throw new BackendException(msg);
            }

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            };

            if (!string.IsNullOrWhiteSpace(_scopes))
            {
                parameters.Add(new KeyValuePair<string, string>("scope", _scopes));
            }

            using var content = new FormUrlEncodedContent(parameters);

            HttpResponseMessage tokenResp;
            try
            {
                tokenResp = await _httpClient.PostAsync(_tokenUrl, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call token endpoint");
                throw new BackendException("Failed to obtain access token");
            }

            var respBody = await tokenResp.Content.ReadAsStringAsync();

            if (!tokenResp.IsSuccessStatusCode)
            {
                _logger.LogError("Token endpoint returned non-success: {Status} {Body}", tokenResp.StatusCode, respBody);
                throw new BackendException("Token endpoint returned an error");
            }

            try
            {
                using var doc = JsonDocument.Parse(respBody);
                if (doc.RootElement.TryGetProperty("access_token", out var at))
                {
                    var token = at.GetString() ?? string.Empty;

                    // Set expiry (default 50 min fallback)
                    if (doc.RootElement.TryGetProperty("expires_in", out var expires))
                    {
                        var expiresIn = expires.GetInt32();
                        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);
                    }
                    else
                    {
                    _tokenExpiry = DateTime.UtcNow.AddMinutes(50);
                    }
                    _cachedToken = token;

                    _logger.LogInformation("Token cached successfully");
                    return token;
                }
                _logger.LogError("access_token not present in token response");
                throw new BackendException("access_token not present in token response");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse token response");
                throw new BackendException("Invalid token response");
            }
        }
    }
}
