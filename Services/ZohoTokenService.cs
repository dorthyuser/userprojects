using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using ZohoOauthKvTest.Models;

namespace ZohoOauthKvTest.Services
{
    public class ZohoTokenService : ITokenService
    {
        // private fields in your token service class:
        private string? _cachedToken = null;
        private string? _refreshToken = null;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SecretClient _secretClient;
        private readonly ILogger<ZohoTokenService> _logger;

        public ZohoTokenService(IHttpClientFactory httpClientFactory, SecretClient secretClient, ILogger<ZohoTokenService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _secretClient = secretClient;
            _logger = logger;
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

                // If token is expired (or expiring in 60s) AND refresh token exists, refresh it
                if (DateTime.UtcNow >= _tokenExpiry.AddSeconds(-60) && !string.IsNullOrEmpty(_refreshToken))
                {
                    await RefreshTokenAsync();
                    return _cachedToken!;
                }

                // Otherwise do full token acquisition
                // Resolve client id, client secret, token url and scopes from Azure Key Vault using TWO-STEP resolution
                var clientIdKey = Environment.GetEnvironmentVariable("ZOHO_CLIENT_ID");
                var clientSecretKey = Environment.GetEnvironmentVariable("ZOHO_CLIENT_SECRET");
                var tokenUrlKey = Environment.GetEnvironmentVariable("AZURE_ZOHO_TOKEN_URL");
                var scopesKey = Environment.GetEnvironmentVariable("ZOHO_SCOPES");

                if (string.IsNullOrEmpty(clientIdKey) || string.IsNullOrEmpty(clientSecretKey) || string.IsNullOrEmpty(tokenUrlKey))
                {
                    throw new InvalidOperationException("Required environment variable for secret key name is not set (ZOHO_CLIENT_ID, ZOHO_CLIENT_SECRET, AZURE_ZOHO_TOKEN_URL)");
                }

                var clientId = (await _secretClient.GetSecretAsync(clientIdKey)).Value.Value;
                var clientSecret = (await _secretClient.GetSecretAsync(clientSecretKey)).Value.Value;
                var tokenUrl = (await _secretClient.GetSecretAsync(tokenUrlKey)).Value.Value;
                var scopes = string.Empty;
                if (!string.IsNullOrEmpty(scopesKey))
                {
                    scopes = (await _secretClient.GetSecretAsync(scopesKey)).Value.Value;
                }

                var http = _httpClientFactory.CreateClient();

                var form = new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret
                };
                if (!string.IsNullOrEmpty(scopes)) form["scope"] = scopes;

                HttpResponseMessage tokenResponseMessage;
                try
                {
                    tokenResponseMessage = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(form));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Token endpoint request failed");
                    throw;
                }

                if (!tokenResponseMessage.IsSuccessStatusCode)
                {
                    var body = await tokenResponseMessage.Content.ReadAsStringAsync();
                    _logger.LogError("Token endpoint returned non-success status {Status}. Body: {Body}", tokenResponseMessage.StatusCode, body);
                    throw new InvalidOperationException("Failed to acquire token from token endpoint");
                }

                var responseJson = await tokenResponseMessage.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseJson);
                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
                {
                    _logger.LogError("Token response invalid: {Response}", responseJson);
                    throw new InvalidOperationException("Invalid token response");
                }

                // after acquiring new token:
                _cachedToken = tokenResponse.access_token;
                _refreshToken = tokenResponse.refresh_token ?? _refreshToken;  // store refresh token if present
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in > 0 ? tokenResponse.expires_in : 3600);

                if (string.IsNullOrEmpty(tokenResponse.refresh_token))
                {
                    _logger.LogWarning("Refresh token missing from token response; OAuth flow may be incomplete.");
                }

                return _cachedToken;
            }
            finally { _tokenLock.Release(); }
        }

        private async Task RefreshTokenAsync()
        {
            try
            {
                // Read refresh token and client credentials from Key Vault (TWO-STEP)
                var refreshTokenKey = Environment.GetEnvironmentVariable("ZOHO_REFRESH_TOKEN");
                var refreshClientIdKey = Environment.GetEnvironmentVariable("ZOHO_CLIENT_ID");
                var refreshClientSecretKey = Environment.GetEnvironmentVariable("ZOHO_CLIENT_SECRET");
                var tokenUrlKey = Environment.GetEnvironmentVariable("AZURE_ZOHO_TOKEN_URL");

                if (string.IsNullOrEmpty(refreshTokenKey) || string.IsNullOrEmpty(refreshClientIdKey) || string.IsNullOrEmpty(refreshClientSecretKey) || string.IsNullOrEmpty(tokenUrlKey))
                {
                    throw new InvalidOperationException("Required environment variable for secret key name is not set for refresh flow (ZOHO_REFRESH_TOKEN, ZOHO_CLIENT_ID, ZOHO_CLIENT_SECRET, AZURE_ZOHO_TOKEN_URL)");
                }

                var refreshToken = (await _secretClient.GetSecretAsync(refreshTokenKey)).Value.Value;
                var refreshClientId = (await _secretClient.GetSecretAsync(refreshClientIdKey)).Value.Value;
                var refreshClientSecret = (await _secretClient.GetSecretAsync(refreshClientSecretKey)).Value.Value;
                var tokenUrl = (await _secretClient.GetSecretAsync(tokenUrlKey)).Value.Value;

                var http = _httpClientFactory.CreateClient();

                var form = new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken,
                    ["client_id"] = refreshClientId,
                    ["client_secret"] = refreshClientSecret
                };

                HttpResponseMessage tokenResponseMessage;
                try
                {
                    tokenResponseMessage = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(form));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Refresh token request failed");
                    throw;
                }

                if (!tokenResponseMessage.IsSuccessStatusCode)
                {
                    var body = await tokenResponseMessage.Content.ReadAsStringAsync();
                    _logger.LogError("Refresh token endpoint returned non-success status {Status}. Body: {Body}", tokenResponseMessage.StatusCode, body);
                    throw new InvalidOperationException("Failed to refresh token");
                }

                var responseJson = await tokenResponseMessage.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseJson);
                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
                {
                    _logger.LogError("Refresh token response invalid: {Response}", responseJson);
                    throw new InvalidOperationException("Invalid refresh token response");
                }

                _cachedToken = tokenResponse.access_token;
                _refreshToken = tokenResponse.refresh_token ?? _refreshToken;  // store refresh token if present
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in > 0 ? tokenResponse.expires_in : 3600);

                if (string.IsNullOrEmpty(tokenResponse.refresh_token))
                {
                    _logger.LogWarning("Refresh token missing from refresh response; OAuth flow may be incomplete.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh access token");
                throw;
            }
        }
    }
}
