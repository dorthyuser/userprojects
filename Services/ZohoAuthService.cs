using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using hello_http_test.Models;

namespace hello_http_test.Services
{
    /// <summary>
    /// Responsible for obtaining and refreshing OAuth2 tokens from Zoho.
    /// Resolves configuration values using IConfiguration or Environment.GetEnvironmentVariable.
    /// </summary>
    public class ZohoAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ZohoAuthService> _logger;
        private TokenResponse _currentToken;
        private readonly object _lock = new object();

        public ZohoAuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ZohoAuthService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Gets a valid access token, refreshing or fetching a new one if necessary.
        /// </summary>
        public async Task<string> GetAccessTokenAsync()
        {
            if (_currentToken != null && !_currentToken.IsExpired())
            {
                return _currentToken.AccessToken;
            }

            lock (_lock)
            {
                if (_currentToken != null && !_currentToken.IsExpired())
                    return _currentToken.AccessToken;
            }

            // Fetch new token using client credentials
            var token = await FetchClientCredentialsTokenAsync();
            lock (_lock)
            {
                _currentToken = token;
            }
            return token.AccessToken;
        }

        /// <summary>
        /// Attempts to refresh the token using the provided refresh token and configured parameters.
        /// </summary>
        public async Task<TokenResponse> RefreshTokenAsync()
        {
            try
            {
                var refreshToken = ResolveValue("Zoho_ref_token");
                if (string.IsNullOrWhiteSpace(refreshToken))
                    throw new InvalidOperationException("Refresh token (Zoho_ref_token) is not configured.");

                var tokenUrl = ResolveValue("zoho_Auth_Token_url");
                var clientId = ResolveValue("zoho_ref_client_id");
                var clientSecret = ResolveValue("zoho_ref_client_secret");
                var grantType = ResolveValue("zoho_ref_GRANT_TYPE");

                var client = _httpClientFactory.CreateClient();
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", grantType },
                    { "refresh_token", refreshToken },
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "oauth_provider", "zoho" }
                });

                var resp = await client.PostAsync(tokenUrl, content);
                resp.EnsureSuccessStatusCode();
                var token = await resp.Content.ReadFromJsonAsync<TokenResponse>();
                token.ObtainedAtUtc = DateTime.UtcNow;
                _logger.LogInformation("Successfully refreshed Zoho token via refresh grant.");
                lock (_lock)
                {
                    _currentToken = token;
                }
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh Zoho token");
                throw;
            }
        }

        private async Task<TokenResponse> FetchClientCredentialsTokenAsync()
        {
            try
            {
                var tokenUrl = ResolveValue("zoho_Auth_Token_url");
                var clientId = ResolveValue("zoho_client_id");
                var clientSecret = ResolveValue("zoho_client_secret");
                var scope = ResolveValue("TOKEN_SCOPE");
                var grantType = "client_credentials";

                var client = _httpClientFactory.CreateClient();
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", grantType },
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "scope", scope }
                });

                var resp = await client.PostAsync(tokenUrl, content);
                resp.EnsureSuccessStatusCode();
                var token = await resp.Content.ReadFromJsonAsync<TokenResponse>();
                token.ObtainedAtUtc = DateTime.UtcNow;
                _logger.LogInformation("Successfully obtained Zoho token via client credentials.");
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to obtain Zoho token via client credentials.");
                throw;
            }
        }

        private string ResolveValue(string key)
        {
            var val = _configuration?[key];
            if (string.IsNullOrWhiteSpace(val))
            {
                val = Environment.GetEnvironmentVariable(key);
            }
            return val;
        }
    }
}
