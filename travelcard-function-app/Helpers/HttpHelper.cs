using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers
{
    public class HttpHelper : IHttpHelper
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public HttpHelper(HttpClient httpClient, ILoggerFactory loggerFactory)
        {
            _httpClient = httpClient;
            _logger = loggerFactory.CreateLogger<HttpHelper>();
        }

        public async Task<string> GetAccessTokenAsync(string tokenUrl, string clientId, string clientSecret, string scope)
        {
            _logger.LogInformation("Requesting access token from {TokenUrl}", tokenUrl);
            try
            {
                var form = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", clientId },
                    { "client_secret", clientSecret }
                };

                if (!string.IsNullOrWhiteSpace(scope))
                {
                    form.Add("scope", scope);
                }

                using var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(form)
                };

                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var resp = await _httpClient.SendAsync(req);
                var content = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Token endpoint returned {Status}: {Content}", resp.StatusCode, content);
                    throw new ApplicationException($"Token endpoint returned {resp.StatusCode}: {content}");
                }

                var doc = JsonSerializer.Deserialize<TokenResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (doc == null || string.IsNullOrWhiteSpace(doc.AccessToken))
                {
                    _logger.LogError("Token response missing access_token: {Content}", content);
                    throw new ApplicationException("Token response missing access_token");
                }

                _logger.LogInformation("Access token retrieved successfully");
                return doc.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching access token");
                throw;
            }
        }
    }
}
