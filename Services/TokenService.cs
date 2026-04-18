using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using ZohoCrmOauthFinal1.Models;

namespace ZohoCrmOauthFinal1.Services
{
    public class TokenService : ITokenService
    {
        private readonly IMemoryCache _cache;
        private readonly SecretClient _secretClient;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<TokenService> _logger;
        private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);
        private const string ACCESS_TOKEN_KEY = "oauth_access_token";
        private const string REFRESH_TOKEN_KEY = "oauth_refresh_token";

        public TokenService(IMemoryCache cache, SecretClient secretClient, IHttpClientFactory httpFactory, ILogger<TokenService> logger)
        {
            _cache = cache;
            _secretClient = secretClient;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (_cache.TryGetValue(ACCESS_TOKEN_KEY, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
                return cachedToken!;

            await _tokenLock.WaitAsync();
            try
            {
                if (_cache.TryGetValue(ACCESS_TOKEN_KEY, out cachedToken) && !string.IsNullOrEmpty(cachedToken))
                    return cachedToken!;

                // Load refresh token: cache first, then secret store
                if (!_cache.TryGetValue(REFRESH_TOKEN_KEY, out string? refreshToken) || string.IsNullOrEmpty(refreshToken))
                {
                    try
                    {
                        var persistedKeyName = Environment.GetEnvironmentVariable("PERSISTED_REFRESH_TOKEN");
                        if (!string.IsNullOrEmpty(persistedKeyName))
                        {
                            var persistedSecret = await _secretClient.GetSecretAsync(persistedKeyName);
                            refreshToken = persistedSecret.Value.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(ex, "No persisted refresh token found in Key Vault or failed to read it");
                    }

                    // If still null, try initial configured refresh token (two-step resolution)
                    if (string.IsNullOrEmpty(refreshToken))
                    {
                        try
                        {
                            var refreshTokenKey = Environment.GetEnvironmentVariable("ZOHO-REFRESH-TOKEN");
                            if (!string.IsNullOrEmpty(refreshTokenKey))
                            {
                                refreshToken = (await _secretClient.GetSecretAsync(refreshTokenKey)).Value.Value;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation(ex, "No initial refresh token configured in Key Vault or failed to read it");
                        }
                    }

                    if (!string.IsNullOrEmpty(refreshToken))
                        _cache.Set(REFRESH_TOKEN_KEY, refreshToken);
                }

                // If refresh token exists → use it to refresh
                if (_cache.TryGetValue(REFRESH_TOKEN_KEY, out string? cachedRefresh) && !string.IsNullOrEmpty(cachedRefresh))
                {
                    await RefreshTokenAsync(cachedRefresh);
                    var newAccess = _cache.Get<string>(ACCESS_TOKEN_KEY);
                    return newAccess ?? throw new InvalidOperationException("Refresh failed to obtain access token");
                }

                // Otherwise, attempt initial authorization code flow exchange if possible
                // Two-step resolution of client id/secret/token url/redirect uri
                var clientIdKey = Environment.GetEnvironmentVariable("ZOHO-CLIENT-ID");
                var clientSecretKey = Environment.GetEnvironmentVariable("ZOHO-CLIENT-SECRET");
                var tokenUrlKey = Environment.GetEnvironmentVariable("ZOHO-TOKEN-URL");
                var redirectUriKey = Environment.GetEnvironmentVariable("ZOHO_REDIRECT_URL");

                if (string.IsNullOrEmpty(clientIdKey) || string.IsNullOrEmpty(clientSecretKey) || string.IsNullOrEmpty(tokenUrlKey) || string.IsNullOrEmpty(redirectUriKey))
                    throw new InvalidOperationException("OAuth configuration is missing. Ensure environment variables are set for OAuth keys.");

                var clientId = (await _secretClient.GetSecretAsync(clientIdKey)).Value.Value;
                var clientSecret = (await _secretClient.GetSecretAsync(clientSecretKey)).Value.Value;
                var tokenUrl = (await _secretClient.GetSecretAsync(tokenUrlKey)).Value.Value;
                var redirectUri = (await _secretClient.GetSecretAsync(redirectUriKey)).Value.Value;

                // Note: No authorization code is provided in this backend service. If a code were available it could be exchanged here.
                throw new InvalidOperationException("No refresh token available and no authorization code exchange is implemented in this service.");
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private async Task RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // Two-step resolution for client id/secret/token url
                var refreshClientIdKey = Environment.GetEnvironmentVariable("ZOHO-CLIENT-ID");
                var refreshClientSecretKey = Environment.GetEnvironmentVariable("ZOHO-CLIENT-SECRET");
                var tokenUrlKey = Environment.GetEnvironmentVariable("ZOHO-TOKEN-URL");

                if (string.IsNullOrEmpty(refreshClientIdKey) || string.IsNullOrEmpty(refreshClientSecretKey) || string.IsNullOrEmpty(tokenUrlKey))
                    throw new InvalidOperationException("Refresh OAuth configuration env vars are not set");

                var clientId = (await _secretClient.GetSecretAsync(refreshClientIdKey)).Value.Value;
                var clientSecret = (await _secretClient.GetSecretAsync(refreshClientSecretKey)).Value.Value;
                var tokenUrl = (await _secretClient.GetSecretAsync(tokenUrlKey)).Value.Value;

                var client = _httpFactory.CreateClient();
                var form = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken },
                    { "client_id", clientId },
                    { "client_secret", clientSecret }
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(form)
                };
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var resp = await client.SendAsync(req);
                var payload = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to refresh token. Status: {Status}. Body: {Body}", resp.StatusCode, payload);
                    throw new InvalidOperationException("Failed to refresh access token");
                }

                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                    throw new InvalidOperationException("Invalid token response from token endpoint");

                var expiresIn = tokenResponse.ExpiresIn > 0 ? tokenResponse.ExpiresIn : 3600;
                _cache.Set(ACCESS_TOKEN_KEY, tokenResponse.AccessToken, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(expiresIn - 60)));

                if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                {
                    _cache.Set(REFRESH_TOKEN_KEY, tokenResponse.RefreshToken);
                    try
                    {
                        var persistedKeyName = Environment.GetEnvironmentVariable("PERSISTED_REFRESH_TOKEN");
                        var secretName = string.IsNullOrEmpty(persistedKeyName) ? "PERSISTED_REFRESH_TOKEN" : persistedKeyName;
                        await _secretClient.SetSecretAsync(secretName, tokenResponse.RefreshToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to persist refresh token to Key Vault");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                throw;
            }
        }
    }
}