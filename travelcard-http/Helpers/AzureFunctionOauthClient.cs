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

namespace travelcard_http.Helpers
{
    /// <summary>
    /// DelegatingHandler that ensures an OAuth2 client_credentials token is fetched and attached to outgoing requests.
    /// Configuration keys read: "$http-client-id", "$http-client-secret", "$http-token-url", "$http-scope".
    /// Token is cached until expiry and retried on 401 to refresh.
    /// </summary>
    public class OAuthDelegatingHandler : DelegatingHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OAuthDelegatingHandler> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private string? _accessToken;
        private DateTimeOffset _accessTokenExpiry = DateTimeOffset.MinValue;

        public OAuthDelegatingHandler(IConfiguration configuration, ILogger<OAuthDelegatingHandler> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await EnsureTokenAsync(cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(_accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Try refresh once
                _logger.LogWarning("Received 401 from backend, attempting token refresh and retry");
                await RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(_accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                }

                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }

        private async Task EnsureTokenAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_accessToken) || DateTimeOffset.UtcNow >= _accessTokenExpiry)
            {
                await RefreshTokenAsync(ct).ConfigureAwait(false);
            }
        }

        private async Task RefreshTokenAsync(CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!string.IsNullOrWhiteSpace(_accessToken) && DateTimeOffset.UtcNow < _accessTokenExpiry)
                {
                    return; // someone else refreshed
                }

                var clientId = _configuration["$http-client-id"] ?? Environment.GetEnvironmentVariable("$http-client-id");
                var clientSecret = _configuration["$http-client-secret"] ?? Environment.GetEnvironmentVariable("$http-client-secret");
                var tokenUrl = _configuration["$http-token-url"] ?? Environment.GetEnvironmentVariable("$http-token-url");
                var scope = _configuration["$http-scope"] ?? Environment.GetEnvironmentVariable("$http-scope");

                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(tokenUrl))
                {
                    _logger.LogError("OAuth configuration is incomplete. Ensure $http-client-id, $http-client-secret, and $http-token-url are set.");
                    throw new InvalidOperationException("OAuth configuration is incomplete.");
                }

                using var http = new HttpClient();
                var body = new FormUrlEncodedContent(new[] {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("scope", scope ?? string.Empty),
                });

                HttpResponseMessage tokenResp;
                try
                {
                    tokenResp = await http.PostAsync(tokenUrl, body, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error requesting OAuth token");
                    throw;
                }

                if (!tokenResp.IsSuccessStatusCode)
                {
                    var txt = await tokenResp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("Token endpoint returned {Status}. Body: {Body}", tokenResp.StatusCode, txt);
                    throw new InvalidOperationException($"Token endpoint returned {tokenResp.StatusCode}");
                }

                var json = await tokenResp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("access_token", out var at))
                    {
                        _accessToken = at.GetString();
                    }

                    if (root.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var secs))
                    {
                        _accessTokenExpiry = DateTimeOffset.UtcNow.AddSeconds(secs - 30);
                    }
                    else
                    {
                        // default to 5 minutes
                        _accessTokenExpiry = DateTimeOffset.UtcNow.AddMinutes(5);
                    }

                    if (string.IsNullOrWhiteSpace(_accessToken))
                    {
                        _logger.LogError("Token endpoint did not provide access_token");
                        throw new InvalidOperationException("Token endpoint did not return access_token");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse token response");
                    throw;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
