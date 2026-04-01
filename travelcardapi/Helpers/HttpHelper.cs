using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TravelcardApi.Models;

namespace TravelcardApi.Helpers
{
    public class HttpHelper : IHttpHelper
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HttpHelper> _logger;
        private readonly string _apiBaseUrl;

        public HttpHelper(IHttpClientFactory httpClientFactory, ILogger<HttpHelper> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiBaseUrl = Environment.GetEnvironmentVariable("TRAVELCARD_API_BASEURL") ?? string.Empty;
        }

        public async Task<HttpResponseMessage> PostTravelcardAsync(TravelcardRequest request, string originalRequestJson)
        {
            if (string.IsNullOrWhiteSpace(_apiBaseUrl))
            {
                throw new InvalidOperationException("TRAVELCARD_API_BASEURL environment variable is not set");
            }

            // Acquire OAuth2 token
            var token = await AcquireTokenAsync(request.Auth.OAuth2);

            var client = _httpClientFactory.CreateClient("travelcard-client");
            var targetUrl = new Uri(new Uri(_apiBaseUrl), "/travelcards");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetUrl)
            {
                Content = new StringContent(originalRequestJson, Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _logger.LogInformation("Sending POST to backend {Url}", targetUrl);

            var response = await client.SendAsync(httpRequest);
            return response;
        }

        private async Task<string> AcquireTokenAsync(OAuth2Settings oauth)
        {
            if (oauth == null) throw new ArgumentNullException(nameof(oauth));
            if (string.IsNullOrWhiteSpace(oauth.ClientId)) throw new InvalidOperationException("OAuth2 client id is required");
            if (string.IsNullOrWhiteSpace(oauth.ClientSecret)) throw new InvalidOperationException("OAuth2 client secret is required");
            if (string.IsNullOrWhiteSpace(oauth.TokenUrl)) throw new InvalidOperationException("OAuth2 token url is required");

            var client = _httpClientFactory.CreateClient("token-client");

            var dict = new Dictionary<string, string>
            {
                { "client_id", oauth.ClientId },
                { "client_secret", oauth.ClientSecret },
                { "grant_type", oauth.GrantType }
            };

            if (oauth.Scopes != null && oauth.Scopes.Count > 0)
            {
                dict["scope"] = string.Join(' ', oauth.Scopes);
            }

            var content = new FormUrlEncodedContent(dict);

            var tokenResponse = await client.PostAsync(oauth.TokenUrl, content);
            var body = await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Token endpoint returned {StatusCode}: {Body}", tokenResponse.StatusCode, body);
                throw new InvalidOperationException($"Token endpoint error: {tokenResponse.StatusCode}");
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("access_token", out var tokenElement))
            {
                throw new InvalidOperationException("access_token not present in token response");
            }

            return tokenElement.GetString() ?? throw new InvalidOperationException("access_token was null");
        }
    }
}
