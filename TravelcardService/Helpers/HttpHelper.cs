using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TravelcardService.Helpers
{
    public class HttpHelper : IHttpHelper
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IKeyVaultService _keyVaultService;
        private readonly ILogger _logger;
        private readonly string _configPath;
        private readonly HttpConnection _connectionConfig;

        public HttpHelper(IHttpClientFactory httpClientFactory, IKeyVaultService keyVaultService, IConfiguration configuration, ILogger<HttpHelper> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var basePath = AppContext.BaseDirectory;
            _configPath = Path.Combine(basePath, "Helpers", "http.json");
            _logger.LogInformation("Resolved http.json path: {Path}", _configPath);
            if (!File.Exists(_configPath))
            {
                throw new FileNotFoundException("http.json configuration not found", _configPath);
            }

            var json = File.ReadAllText(_configPath);
            var doc = JsonSerializer.Deserialize<HttpFile>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (doc == null || doc.Connections == null || doc.Connections.Length == 0)
            {
                throw new InvalidOperationException("Invalid http.json contents");
            }

            // For this project we assume single connection named travelcard-api-http
            _connectionConfig = doc.Connections[0];
            _logger.LogInformation("HttpHelper initialized with baseUrl {BaseUrl}", _connectionConfig.BaseUrl);
        }

        public async Task<HttpResponseMessage> PostToTravelcardAsync(string rawJson)
        {
            _logger.LogInformation("Enter HttpHelper.PostToTravelcardAsync");

            var client = _httpClientFactory.CreateClient("travelcard-client");
            client.Timeout = TimeSpan.FromSeconds(60);

            var endpointPath = "api/travelcard";
            var url = _connectionConfig.BaseUrl?.TrimEnd('/') + "/" + endpointPath;

            // Determine auth: prefer oauth2 resource if present
            string? clientId = null;
            string? clientSecret = null;
            string? tokenUrl = null;
            string? bearerToken = null;

            foreach (var resource in _connectionConfig.Resources)
            {
                if (resource.AuthMethod?.Equals("oauth2", StringComparison.OrdinalIgnoreCase) == true && resource.Auth?.Oauth2 != null)
                {
                    // resolve secrets via KeyVault if placeholder-like
                    clientId = resource.Auth.Oauth2.ClientId;
                    clientSecret = resource.Auth.Oauth2.ClientSecret;
                    tokenUrl = resource.Auth.Oauth2.TokenUrl;

                    if (!string.IsNullOrEmpty(clientId) && clientId.StartsWith("AZURE-"))
                    {
                        var secretName = clientId;
                        clientId = await _keyVaultService.GetSecretAsync(secretName);
                    }

                    if (!string.IsNullOrEmpty(clientSecret) && clientSecret.StartsWith("AZURE-"))
                    {
                        var secretName = clientSecret;
                        clientSecret = await _keyVaultService.GetSecretAsync(secretName);
                    }

                    if (!string.IsNullOrEmpty(tokenUrl) && tokenUrl.StartsWith("AZURE-"))
                    {
                        tokenUrl = await _keyVaultService.GetSecretAsync(tokenUrl);
                    }

                    // Acquire token
                    if (!string.IsNullOrEmpty(tokenUrl) && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
                    {
                        bearerToken = await AcquireOAuth2TokenAsync(tokenUrl, clientId, clientSecret);
                    }

                    break;
                }
            }

            // If bearerToken still null, try to look for basic or direct credentials
            string? clientIdHeaderValue = null;
            if (!string.IsNullOrEmpty(clientId))
            {
                clientIdHeaderValue = clientId;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(rawJson ?? string.Empty, Encoding.UTF8, "application/json")
            };

            // Required headers
            if (!string.IsNullOrEmpty(clientIdHeaderValue))
            {
                request.Headers.Add("client_id", clientIdHeaderValue);
            }

            request.Headers.Authorization = bearerToken != null ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            _logger.LogInformation("Forwarding request to {Url} with headers client_id:{ClientId} Authorization: {HasAuth}", url, clientIdHeaderValue ?? "(none)", bearerToken != null);

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending HTTP request to backend");
                throw new BackendException("Failed to call backend API", ex.Message, 0);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Backend returned non-success status {Status}. Body: {Body}", (int)response.StatusCode, body);
                throw new BackendException("Backend returned error", body, (int)response.StatusCode);
            }

            _logger.LogInformation("Exit HttpHelper.PostToTravelcardAsync with success status {Status}", (int)response.StatusCode);
            return response;
        }

        private async Task<string> AcquireOAuth2TokenAsync(string tokenUrl, string clientId, string clientSecret)
        {
            _logger.LogInformation("Acquiring OAuth2 token from {TokenUrl}", tokenUrl);
            try
            {
                using var client = new HttpClient();
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret)
                });

                var resp = await client.PostAsync(tokenUrl, content);
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Token endpoint returned {Status}. Body: {Body}", (int)resp.StatusCode, body);
                    throw new BackendException("Token endpoint error", body, (int)resp.StatusCode);
                }

                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("access_token", out var tokenEl))
                {
                    return tokenEl.GetString() ?? string.Empty;
                }

                _logger.LogError("access_token not found in token response: {Body}", body);
                throw new BackendException("Token response missing access_token", body, (int)resp.StatusCode);
            }
            catch (BackendException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while acquiring token");
                throw new BackendException("Failed to acquire token", ex.Message, 0);
            }
        }
    }

    // Config models for http.json parsing
    public record HttpFile(HttpConnection[] Connections);
    public record HttpConnection(string Name, string Protocol, string BaseUrl, Resource[] Resources);
    public record Resource(string Endpoints, string AuthMethod, AuthWrapper? Auth);
    public record AuthWrapper(BasicAuth? Basic, OAuth2Auth? Oauth2);
    public record BasicAuth(string Username, string Password);
    public record OAuth2Auth(string ClientId, string ClientSecret, string TokenUrl, string GrantType, string Scopes, string? AuthorizationUrl);
}
