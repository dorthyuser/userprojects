using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace hello_http_test.Services
{
    internal class TokenResponse
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
    }

    public class TokenService : ITokenService
    {
        private readonly SecretClient _secretClient;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<TokenService> _logger;
        private readonly object _sync = new object();
        private string _accessToken;
        private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

        public TokenService(SecretClient secretClient, IHttpClientFactory httpFactory, ILogger<TokenService> logger)
        {
            _secretClient = secretClient;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && _expiresAt > DateTimeOffset.UtcNow.AddSeconds(60))
            {
                return _accessToken;
            }

            // Lock to ensure single fetch/refresh
            lock (_sync)
            {
                if (!string.IsNullOrEmpty(_accessToken) && _expiresAt > DateTimeOffset.UtcNow.AddSeconds(60))
                {
                    return _accessToken;
                }
            }

            // Attempt to fetch secrets and request token
            try
            {
                // Primary client credentials keys
                var clientIdSecret = _secretClient.GetSecret("zoho_client_id").Value?.Value;
                var clientSecret = _secretClient.GetSecret("zoho_client_secret").Value?.Value;
                var tokenUrl = _secretClient.GetSecret("zoho_Auth_Token_url").Value?.Value;
                var scope = _secretClient.GetSecret("TOKEN_SCOPE").Value?.Value;

                if (string.IsNullOrEmpty(tokenUrl) || string.IsNullOrEmpty(clientIdSecret) || string.IsNullOrEmpty(clientSecret))
                {
                    throw new InvalidOperationException("Required OAuth2 secrets are not available in Key Vault.");
                }

                // Use client credentials flow
                var token = RequestClientCredentialsTokenAsync(tokenUrl, clientIdSecret, clientSecret, scope).GetAwaiter().GetResult();

                if (token == null || string.IsNullOrEmpty(token.access_token))
                {
                    throw new InvalidOperationException("Failed to acquire access token.");
                }

                lock (_sync)
                {
                    _accessToken = token.access_token;
                    _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.expires_in);
                }

                return _accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acquiring token");
                throw;
            }
        }

        private async Task<TokenResponse> RequestClientCredentialsTokenAsync(string tokenUrl, string clientId, string clientSecret, string scope)
        {
            using var http = _httpFactory.CreateClient();
            var dict = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            };

            if (!string.IsNullOrEmpty(scope))
            {
                dict.Add(new KeyValuePair<string, string>("scope", scope));
            }

            using var content = new FormUrlEncodedContent(dict);

            var resp = await http.PostAsync(tokenUrl, content);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                _logger.LogError("Token endpoint returned {Status}: {Body}", resp.StatusCode, err);
                resp.EnsureSuccessStatusCode();
            }

            var json = await resp.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return token;
        }
    }
}
