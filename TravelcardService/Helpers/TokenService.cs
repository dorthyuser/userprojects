using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TravelcardService.Helpers
{
    public class TokenService : ITokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IKeyVaultHelper _keyVaultHelper;
        private readonly ILogger _logger;
        private string? _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public TokenService(HttpClient httpClient, IKeyVaultHelper keyVaultHelper, ILoggerFactory loggerFactory)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _keyVaultHelper = keyVaultHelper ?? throw new ArgumentNullException(nameof(keyVaultHelper));
            _logger = loggerFactory?.CreateLogger<TokenService>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task<string> GetTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            {
                _logger.LogDebug("Using cached access token");
                return _cachedToken;
            }

            _logger.LogInformation("Requesting new access token from token endpoint");

            string clientId = await _keyVaultHelper.GetSecretAsync("AZURE-CLIENT-ID").ConfigureAwait(false) ?? throw new InvalidOperationException("AZURE-CLIENT-ID not found in Key Vault");
            string clientSecret = await _keyVaultHelper.GetSecretAsync("AZURE-CLIENT-SECRET").ConfigureAwait(false) ?? throw new InvalidOperationException("AZURE-CLIENT-VALUE not found in Key Vault");
            string tokenUrl = await _keyVaultHelper.GetSecretAsync("AZURE-TOKEN-URL").ConfigureAwait(false) ?? throw new InvalidOperationException("AZURE-TOKEN-URL not found in Key Vault");
            string scopes = await _keyVaultHelper.GetSecretAsync("AZURE-SCOPES").ConfigureAwait(false) ?? string.Empty;

            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = new StringContent($"client_id={Uri.EscapeDataString(clientId)}&client_secret={Uri.EscapeDataString(clientSecret)}&grant_type=client_credentials&scope={Uri.EscapeDataString(scopes)}", Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            string resp = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token endpoint returned non-success status {StatusCode}: {Response}", response.StatusCode, resp);
                throw new Exception("Failed to acquire access token: " + resp);
            }

            using var doc = JsonDocument.Parse(resp);
            if (!doc.RootElement.TryGetProperty("access_token", out var at))
            {
                _logger.LogError("Token response did not contain access_token. Response: {Response}", resp);
                throw new Exception("Invalid token response");
            }

            _cachedToken = at.GetString() ?? string.Empty;

            if (doc.RootElement.TryGetProperty("expires_in", out var expiresInEl) && expiresInEl.TryGetInt32(out var expiresIn))
            {
                _tokenExpiry = DateTime.UtcNow.AddSeconds(Math.Max(30, expiresIn - 30));
            }
            else
            {
                _tokenExpiry = DateTime.UtcNow.AddMinutes(5);
            }

            _logger.LogInformation("Acquired access token, expires at {Expiry}", _tokenExpiry);
            return _cachedToken;
        }
    }
}
