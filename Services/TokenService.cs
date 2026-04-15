using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace hello_http_test.Services
{
    /// <summary>
    /// Manages OAuth2 tokens for Zoho using client credentials and refresh token flows.
    /// Resolves configuration values from Environment variables first, then IConfiguration.
    /// </summary>
    public class TokenService
    {
        private readonly IHttpClientFactory _factory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;
        private string _accessToken;
        private DateTime _expiresAt = DateTime.MinValue;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Constructor.
        /// </summary>
        public TokenService(IHttpClientFactory factory, IConfiguration configuration, ILogger<TokenService> logger)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string Resolve(string key)
        {
            return Environment.GetEnvironmentVariable(key) ?? _configuration[key];
        }

        /// <summary>
        /// Gets a valid access token. Performs acquisition or refresh as needed.
        /// </summary>
        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresAt)
            {
                return _accessToken;
            }

            await _semaphore.WaitAsync();
            try
            {
                if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresAt)
                {
                    return _accessToken;
                }

                await AcquireTokenAsync();
                return _accessToken;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Forces a token refresh (used when a 401 is received).
        /// </summary>
        public async Task ForceRefreshAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                await RefreshTokenAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task AcquireTokenAsync()
        {
            var tokenUrl = Resolve("zoho_Auth_Token_url");
            if (string.IsNullOrWhiteSpace(tokenUrl)) throw new InvalidOperationException("zoho_Auth_Token_url not configured.");

            var clientId = Resolve("zoho_client_id");
            var clientSecret = Resolve("zoho_client_secret");
            var scope = Resolve("TOKEN_SCOPE");

            var client = _factory.CreateClient("zoho-token-client");

            var form = new MultipartFormDataContent
            {
                { new StringContent("client_credentials"), "grant_type" },
                { new StringContent(clientId ?? string.Empty), "client_id" },
                { new StringContent(clientSecret ?? string.Empty), "client_secret" },
                { new StringContent(scope ?? string.Empty), "scope" }
            };

            HttpResponseMessage resp;
            try
            {
                resp = await client.PostAsync(tokenUrl, form);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token acquisition request failed.");
                throw;
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogError("Token endpoint returned non-success: {Status} {Body}", resp.StatusCode, body);
                throw new InvalidOperationException("Failed to acquire token.");
            }

            var payload = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (payload.RootElement.TryGetProperty("access_token", out var at))
            {
                _accessToken = at.GetString();
                if (payload.RootElement.TryGetProperty("expires_in", out var exp))
                {
                    var seconds = exp.GetInt32();
                    _expiresAt = DateTime.UtcNow.AddSeconds(seconds - 30);
                }
                else
                {
                    _expiresAt = DateTime.UtcNow.AddMinutes(5);
                }
            }
            else
            {
                _logger.LogError("Token endpoint did not return access_token.");
                throw new InvalidOperationException("No access_token in token response.");
            }
        }

        private async Task RefreshTokenAsync()
        {
            var tokenUrl = Resolve("zoho_Auth_Token_url");
            if (string.IsNullOrWhiteSpace(tokenUrl)) throw new InvalidOperationException("zoho_Auth_Token_url not configured.");

            var refreshToken = Resolve("Zoho_ref_token");
            var grantType = Resolve("zoho_ref_GRANT_TYPE");
            var refreshClientId = Resolve("zoho_ref_client_id");
            var refreshClientSecret = Resolve("zoho_ref_client_secret");

            var client = _factory.CreateClient("zoho-token-client");

            var form = new MultipartFormDataContent
            {
                { new StringContent(grantType ?? string.Empty), "grant_type" },
                { new StringContent(refreshToken ?? string.Empty), "refresh_token" },
                { new StringContent(refreshClientId ?? string.Empty), "client_id" },
                { new StringContent(refreshClientSecret ?? string.Empty), "client_secret" },
                { new StringContent("zoho"), "oauth_provider" }
            };

            HttpResponseMessage resp;
            try
            {
                resp = await client.PostAsync(tokenUrl, form);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh request failed.");
                throw;
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogError("Refresh token endpoint returned non-success: {Status} {Body}", resp.StatusCode, body);
                throw new InvalidOperationException("Failed to refresh token.");
            }

            var payload = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (payload.RootElement.TryGetProperty("access_token", out var at))
            {
                _accessToken = at.GetString();
                if (payload.RootElement.TryGetProperty("expires_in", out var exp))
                {
                    var seconds = exp.GetInt32();
                    _expiresAt = DateTime.UtcNow.AddSeconds(seconds - 30);
                }
                else
                {
                    _expiresAt = DateTime.UtcNow.AddMinutes(5);
                }
            }
            else
            {
                _logger.LogError("Refresh endpoint did not return access_token.");
                throw new InvalidOperationException("No access_token in refresh response.");
            }
        }
    }
}
