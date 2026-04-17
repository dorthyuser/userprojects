using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using ZohoCrmOauthFinal.Models;

namespace ZohoCrmOauthFinal.Services
{
    public class TokenService : ITokenService
    {
        private readonly SecretClient _secretClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TokenService> _logger;

        // private fields in your token service class:
        private string? _cachedToken = null;
        private string? _refreshToken = null;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);

        public TokenService(SecretClient secretClient, IHttpClientFactory httpClientFactory, ILogger<TokenService> logger)
        {
            _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry.AddSeconds(-60))
                return _cachedToken;

            await _tokenLock.WaitAsync();
            try
            {
                // double-check inside lock
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry.AddSeconds(-60))
                    return _cachedToken;

                // Ensure refresh token is loaded from Key Vault (TWO-STEP)
                if (string.IsNullOrEmpty(_refreshToken))
                {
                    var refreshTokenKey = Environment.GetEnvironmentVariable("ZOHO_REFRESH_TOKEN");
                    if (!string.IsNullOrEmpty(refreshTokenKey))
                    {
                        try
                        {
                            _refreshToken = (await _secretClient.GetSecretAsync(refreshTokenKey)).Value.Value;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Unable to read ZOHO_REFRESH_TOKEN from Key Vault");
                        }
                    }
                }

                // If token is expired (or expiring in 60s) AND refresh token exists, refresh it
                if (DateTime.UtcNow >= _tokenExpiry.AddSeconds(-60) && !string.IsNullOrEmpty(_refreshToken))
                {
                    await RefreshTokenAsync();
                    return _cachedToken ?? string.Empty;
                }

                // Otherwise do full token acquisition (authorization_code flow)
                // Attempt to perform authorization_code exchange if an auth code is available in Key Vault
                var authCodeKey = Environment.GetEnvironmentVariable("ZOHO_AUTH_CODE");
                if (!string.IsNullOrEmpty(authCodeKey))
                {
                    string authCode = string.Empty;
                    try
                    {
                        authCode = (await _secretClient.GetSecretAsync(authCodeKey)).Value.Value;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Unable to read ZOHO_AUTH_CODE from Key Vault");
                    }

                    if (!string.IsNullOrEmpty(authCode))
                    {
                        // Read required secrets
                        var clientIdKey = Environment.GetEnvironmentVariable("ZOHO_CLIENT_ID");
                        var clientSecretKey = Environment.GetEnvironmentVariable("ZOHO_CLIENT_SECRET");
                        var tokenUrlKey = Environment.GetEnvironmentVariable("ZOHO_TOKEN_URL");
                        var redirectUriKey = Environment.GetEnvironmentVariable("ZOHO_REDIRECT_URL");

                        if (string.IsNullOrEmpty(clientIdKey) || string.IsNullOrEmpty(clientSecretKey) || string.IsNullOrEmpty(tokenUrlKey) || string.IsNullOrEmpty(redirectUriKey))
                            throw new InvalidOperationException("Required OAuth keys are not configured in environment variables");

                        var clientId = (await _secretClient.GetSecretAsync(clientIdKey)).Value.Value;
                        var clientSecret = (await _secretClient.GetSecretAsync(clientSecretKey)).Value.Value;
                        var tokenUrl = (await _secretClient.GetSecretAsync(tokenUrlKey)).Value.Value;
                        var redirectUri = (await _secretClient.GetSecretAsync(redirectUriKey)).Value.Value;

                        var http = _httpClientFactory.CreateClient();

                        var form = new Dictionary<string, string>
                        {
                            ["grant_type"] = "authorization_code",
                            ["code"] = authCode,
                            ["client_id"] = clientId,
                            ["client_secret"] = clientSecret,
                            ["redirect_uri"] = redirectUri,
                            ["scope"] = "ZohoCRM.users.ALL"
                        };

                        HttpResponseMessage resp;
                        try
                        {
                            resp = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(form));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Token endpoint request failed");
                            throw;
                        }

                        if (!resp.IsSuccessStatusCode)
                        {
                            var body = await resp.Content.ReadAsStringAsync();
                            _logger.LogError("Token endpoint returned non-success: {Status} {Body}", resp.StatusCode, body);
                            throw new InvalidOperationException("Failed to acquire token");
                        }

                        var content = await resp.Content.ReadAsStringAsync();
                        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

                        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
                            throw new InvalidOperationException("Token response invalid");

                        // after acquiring new token:
                        _cachedToken = tokenResponse.access_token;
                        _refreshToken = tokenResponse.refresh_token ?? _refreshToken;  // store refresh token if present
                        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in > 0 ? tokenResponse.expires_in : 3600);

                        if (string.IsNullOrEmpty(tokenResponse.refresh_token))
                            _logger.LogWarning("Token response did not contain a refresh_token; OAuth flow may be incomplete.");

                        return _cachedToken;
                    }
                }

                // If we reach here and there is a refresh token, attempt refresh
                if (!string.IsNullOrEmpty(_refreshToken))
                {
                    await RefreshTokenAsync();
                    return _cachedToken ?? string.Empty;
                }

                throw new InvalidOperationException("No means to acquire access token: no auth code or refresh token available");
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private async Task RefreshTokenAsync()
        {
            try
            {
                var refreshClientIdKey = Environment.GetEnvironmentVariable("ZOHO_CLIENT_ID");
                var refreshClientSecretKey = Environment.GetEnvironmentVariable("ZOHO_CLIENT_SECRET");
                var tokenUrlKey = Environment.GetEnvironmentVariable("ZOHO_TOKEN_URL");

                if (string.IsNullOrEmpty(refreshClientIdKey) || string.IsNullOrEmpty(refreshClientSecretKey) || string.IsNullOrEmpty(tokenUrlKey))
                    throw new InvalidOperationException("Refresh token keys not configured in environment variables");

                var refreshClientId = (await _secretClient.GetSecretAsync(refreshClientIdKey)).Value.Value;
                var refreshClientSecret = (await _secretClient.GetSecretAsync(refreshClientSecretKey)).Value.Value;
                var tokenUrl = (await _secretClient.GetSecretAsync(tokenUrlKey)).Value.Value;

                var http = _httpClientFactory.CreateClient();

                var form = new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = _refreshToken ?? string.Empty,
                    ["client_id"] = refreshClientId,
                    ["client_secret"] = refreshClientSecret
                };

                HttpResponseMessage resp;
                try
                {
                    resp = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(form));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Refresh token request failed");
                    throw;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    _logger.LogError("Refresh token endpoint returned non-success: {Status} {Body}", resp.StatusCode, body);
                    throw new InvalidOperationException("Failed to refresh token");
                }

                var content = await resp.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
                    throw new InvalidOperationException("Refresh token response invalid");

                // after acquiring new token:
                _cachedToken = tokenResponse.access_token;
                _refreshToken = tokenResponse.refresh_token ?? _refreshToken;  // store refresh token if present
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in > 0 ? tokenResponse.expires_in : 3600);

                if (string.IsNullOrEmpty(tokenResponse.refresh_token))
                    _logger.LogWarning("Refresh response did not contain a refresh_token; OAuth flow may be incomplete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh token");
                throw;
            }
        }
    }
}
