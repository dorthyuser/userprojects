using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace travelcard_service.Helpers
{
    public class OAuthTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OAuthTokenService> _logger;

        public OAuthTokenService(HttpClient httpClient, ILogger<OAuthTokenService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TokenResult> GetAccessTokenAsync()
        {
            try
            {
                var clientId = "6e0541cd-1228-4808-9296-9168b51eff87"; //Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
                var clientSecret = "j2_8Q~rRE3Fb.OnjHawA8971_2yKKGQBE8XWVbHN"; //Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
                var tokenUrl = "https://login.microsoftonline.com/f511e351-8335-4819-96aa-66d1ad695f56/oauth2/v2.0/token"; //Environment.GetEnvironmentVariable("AZURE_TOKEN_URL");
                var scopes = "api://b4602da5-c997-4bb3-8bfb-a97461954bff/.default"; //Environment.GetEnvironmentVariable("AZURE_SCOPES");

                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(tokenUrl))
                {
                    var missing = "One or more OAuth environment variables are missing: AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TOKEN_URL";
                    _logger.LogError(missing);
                    return new TokenResult { Success = false, Error = missing };
                }

                var form = new Dictionary<string, string>
                {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "grant_type", "client_credentials" }
                };

                if (!string.IsNullOrWhiteSpace(scopes))
                {
                    form.Add("scope", scopes);
                }

                using var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(form)
                };

                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(req);

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Token endpoint returned non-success status {Status}. Content: {Content}", (int)response.StatusCode, content);
                    return new TokenResult { Success = false, Error = content, StatusCode = (int)response.StatusCode };
                }

                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
                {
                    var token = tokenElement.GetString();
                    _logger.LogInformation("acess token Acquired from OAuth service {Token} ", token);
                    return new TokenResult { Success = true, AccessToken = token };
                    
                }

                _logger.LogError("Token response did not contain access_token. Raw: {Content}", content);
                return new TokenResult { Success = false, Error = "access_token missing in token response", Details = content };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while acquiring access token");
                return new TokenResult { Success = false, Error = ex.Message };
            }
        }
    }

    public class TokenResult
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? Error { get; set; }
        public object? Details { get; set; }
        public int StatusCode { get; set; }
    }
}
