using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using tc_testing_api2.Models;

namespace tc_testing_api2.Services
{
    /// <summary>
    /// DelegatingHandler that ensures an OAuth2 token is available and attaches it as a Bearer header.
    /// It refreshes token on expiry and retries once on 401.
    /// </summary>
    public class TokenDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenDelegatingHandler> _logger;
        private string? _accessToken;
        private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Constructor. Uses IHttpClientFactory and IConfiguration resolved via DI.
        /// </summary>
        public TokenDelegatingHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TokenDelegatingHandler> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await EnsureTokenAsync(cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(_accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogInformation("Received 401, attempting token refresh and retry");
                await ForceRefreshTokenAsync(cancellationToken).ConfigureAwait(false);
                // Replace Authorization header
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                response.Dispose();
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }

        private async Task EnsureTokenAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_accessToken) || DateTimeOffset.UtcNow >= _expiresAt)
            {
                await RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ForceRefreshTokenAsync(CancellationToken cancellationToken)
        {
            await RefreshTokenAsync(cancellationToken, force: true).ConfigureAwait(false);
        }

        private async Task RefreshTokenAsync(CancellationToken cancellationToken, bool force = false)
        {
            try
            {
                if (!force && _accessToken != null && DateTimeOffset.UtcNow < _expiresAt)
                    return;

                await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (!force && _accessToken != null && DateTimeOffset.UtcNow < _expiresAt)
                        return;

                    var tokenUrl = _configuration["AZURE-TOKEN-URL"] ?? Environment.GetEnvironmentVariable("AZURE-TOKEN-URL");
                    var clientId = _configuration["AZURE-CLIENT-ID"] ?? Environment.GetEnvironmentVariable("AZURE-CLIENT-ID");
                    var clientSecret = _configuration["AZURE-CLIENT-SECRET"] ?? Environment.GetEnvironmentVariable("AZURE-CLIENT-SECRET");
                    var scopes = _configuration["AZURE-SCOPES"] ?? Environment.GetEnvironmentVariable("AZURE-SCOPES");

                    if (string.IsNullOrEmpty(tokenUrl) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                    {
                        throw new TokenException("OAuth2 configuration is incomplete. Ensure AZURE-TOKEN-URL, AZURE-CLIENT-ID and AZURE-CLIENT-SECRET are set.");
                    }

                    var client = _httpClientFactory.CreateClient("token-client");
                    var form = new Dictionary<string, string>
                    {
                        {"grant_type", "client_credentials"},
                        {"client_id", clientId},
                        {"client_secret", clientSecret}
                    };

                    if (!string.IsNullOrEmpty(scopes))
                        form["scope"] = scopes;

                    var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl) { Content = new FormUrlEncodedContent(form) };
                    var resp = await client.SendAsync(req, cancellationToken).ConfigureAwait(false);
                    var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogError("Token endpoint returned non-success: {Status} - {Body}", resp.StatusCode, body);
                        throw new TokenException($"Token endpoint error: {resp.StatusCode}");
                    }

                    var json = JsonSerializer.Deserialize<TokenResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (json == null || string.IsNullOrEmpty(json.AccessToken))
                    {
                        _logger.LogError("Token response did not contain access token. Body: {Body}", body);
                        throw new TokenException("Token response missing access token");
                    }

                    _accessToken = json.AccessToken;
                    var expires = json.ExpiresIn > 0 ? json.ExpiresIn : 3600;
                    _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expires - 60);
                }
                finally
                {
                    _refreshLock.Release();
                }
            }
            catch (TokenException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh token");
                throw new TokenException("Failed to acquire token", ex);
            }
        }
    }
}