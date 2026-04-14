using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace tc_csharp_api
{
    /// <summary>
    /// Responsible for acquiring and caching OAuth2 access tokens using client credentials flow.
    /// All secret and configuration values are resolved from IConfiguration or environment variables.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ISecretProvider _secretProvider;
        private readonly ILogger<TokenService> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private string? _accessToken;
        private DateTime _expiresAt = DateTime.MinValue;

        public TokenService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ISecretProvider secretProvider, ILogger<TokenService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _secretProvider = secretProvider;
            _logger = logger;
        }

        public async Task<string> GetTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresAt)
            {
                return _accessToken!;
            }

            await _semaphore.WaitAsync();
            try
            {
                if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresAt)
                {
                    return _accessToken!;
                }

                await AcquireTokenAsync();

                if (string.IsNullOrEmpty(_accessToken))
                {
                    throw new TokenException("Failed to obtain access token");
                }

                return _accessToken!;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ForceRefreshAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                _accessToken = null;
                await AcquireTokenAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task AcquireTokenAsync()
        {
            // Resolve secret name and then attempt to read credentials from a secret provider or env/config
            var secretName = Environment.GetEnvironmentVariable("OAUTH_SECRET_NAME") ?? _configuration["OAUTH_SECRET_NAME"];

            var clientId = Environment.GetEnvironmentVariable("AZURE-CLIENT-ID") ?? _configuration["AZURE-CLIENT-ID"];
            var clientSecret = Environment.GetEnvironmentVariable("AZURE-CLIENT-SECRET") ?? _configuration["AZURE-CLIENT-SECRET"];
            var tokenUrl = Environment.GetEnvironmentVariable("AZURE-TOKEN-URL") ?? _configuration["AZURE-TOKEN-URL"];
            var scopes = Environment.GetEnvironmentVariable("AZURE-SCOPES") ?? _configuration["AZURE-SCOPES"];

            // If a secret name is provided, attempt to fetch values from secret provider (not hardcoded)
            if (!string.IsNullOrEmpty(secretName))
            {
                try
                {
                    var resolved = await _secretProvider.GetSecretAsync(secretName);
                    if (string.IsNullOrEmpty(clientId) && resolved.TryGetValue("AZURE-CLIENT-ID", out var sid)) clientId = sid;
                    if (string.IsNullOrEmpty(clientSecret) && resolved.TryGetValue("AZURE-CLIENT-SECRET", out var ssecret)) clientSecret = ssecret;
                    if (string.IsNullOrEmpty(tokenUrl) && resolved.TryGetValue("AZURE-TOKEN-URL", out var stoken)) tokenUrl = stoken;
                    if (string.IsNullOrEmpty(scopes) && resolved.TryGetValue("AZURE-SCOPES", out var sscopes)) scopes = sscopes;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch secrets from secret provider for name {SecretName}", secretName);
                }
            }

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tokenUrl))
            {
                _logger.LogError("OAuth configuration incomplete. Ensure AZURE-CLIENT-ID, AZURE-CLIENT-SECRET and AZURE-TOKEN-URL are provided via configuration or secret provider.");
                throw new TokenException("OAuth configuration is incomplete");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("token-client");

                var dict = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", clientId },
                    { "client_secret", clientSecret }
                };

                if (!string.IsNullOrEmpty(scopes))
                {
                    dict["scope"] = scopes;
                }

                var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(dict)
                };

                var resp = await client.SendAsync(req);
                var raw = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Token endpoint returned non-success status {Status} with content: {Content}", resp.StatusCode, raw);
                    throw new TokenException($"Token endpoint returned {resp.StatusCode}: {raw}");
                }

                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("access_token", out var at))
                {
                    _accessToken = at.GetString();
                }

                if (doc.RootElement.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var secs))
                {
                    _expiresAt = DateTime.UtcNow.AddSeconds(secs - 30); // refresh a bit earlier
                }
                else
                {
                    _expiresAt = DateTime.UtcNow.AddMinutes(5);
                }

                if (string.IsNullOrEmpty(_accessToken))
                {
                    _logger.LogError("Token response did not contain an access_token: {Raw}", raw);
                    throw new TokenException("Token response did not contain access_token");
                }
            }
            catch (TokenException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire token");
                throw new TokenException("Failed to acquire token: " + ex.Message);
            }
        }
    }
}
