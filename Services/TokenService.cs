using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TcTestingZoho.Models;

namespace TcTestingZoho.Services
{
    public class TokenException : Exception
    {
        public TokenException(string message) : base(message) { }
        public TokenException(string message, Exception inner) : base(message, inner) { }
    }

    public class TokenService : ITokenService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TokenService> _logger;
        private readonly string _tokenEndpoint;
        private readonly string _clientIdEnvVarName;
        private readonly string _clientSecretEnvVarName;
        private readonly string _scope;
        private readonly string _clientIdValue;
        private readonly string _clientSecretValue;

        public TokenService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TokenService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            _tokenEndpoint = configuration["OAuth:TokenEndpoint"] ?? string.Empty;
            _clientIdEnvVarName = configuration["OAuth:ClientIdEnvVar"] ?? "OAUTH_CLIENT_ID";
            _clientSecretEnvVarName = configuration["OAuth:ClientSecretEnvVar"] ?? "OAUTH_CLIENT_SECRET";
            _scope = configuration["OAuth:Scope"] ?? string.Empty;

            if (string.IsNullOrEmpty(_tokenEndpoint))
            {
                _logger.LogWarning("OAuth:TokenEndpoint is not configured.");
            }

            _clientIdValue = Environment.GetEnvironmentVariable(_clientIdEnvVarName) ?? string.Empty;
            _clientSecretValue = Environment.GetEnvironmentVariable(_clientSecretEnvVarName) ?? string.Empty;

            if (string.IsNullOrEmpty(_clientIdValue))
            {
                _logger.LogWarning("ClientId environment variable '{env}' is not set.", _clientIdEnvVarName);
            }

            if (string.IsNullOrEmpty(_clientSecretValue))
            {
                _logger.LogWarning("ClientSecret environment variable '{env}' is not set.", _clientSecretEnvVarName);
            }
        }

        public string GetClientId()
        {
            return _clientIdValue;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(_tokenEndpoint))
            {
                throw new TokenException("Token endpoint is not configured.");
            }

            if (string.IsNullOrEmpty(_clientIdValue) || string.IsNullOrEmpty(_clientSecretValue))
            {
                throw new TokenException("Client credentials are not available in environment variables.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("OAuthClient");

                var parameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", _clientIdValue),
                    new KeyValuePair<string, string>("client_secret", _clientSecretValue)
                };

                if (!string.IsNullOrEmpty(_scope))
                {
                    parameters.Add(new KeyValuePair<string, string>("scope", _scope));
                }

                using var content = new FormUrlEncodedContent(parameters);
                using var response = await client.PostAsync(_tokenEndpoint, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Token endpoint returned non-success status {status}. Response: {response}", response.StatusCode, responseContent);
                    throw new TokenException($"Failed to obtain access token. Status: {response.StatusCode}");
                }

                var token = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (token == null || string.IsNullOrEmpty(token.AccessToken))
                {
                    _logger.LogError("Token endpoint returned invalid payload: {payload}", responseContent);
                    throw new TokenException("Token endpoint returned invalid payload.");
                }

                return token.AccessToken;
            }
            catch (TokenException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while obtaining access token.");
                throw new TokenException("Exception while obtaining access token.", ex);
            }
        }
    }
}
