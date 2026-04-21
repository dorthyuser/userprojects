using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using zoho_project_csharp.Models;

namespace zoho_project_csharp.Services
{
    public class ZohoCrmConnection : IZohoCrmConnection
    {
        private readonly HttpClient _apiClient;
        private readonly HttpClient _tokenClient;
        private readonly ZohoOptions _options;
        private readonly ILogger<ZohoCrmConnection> _logger;

        private string? _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private string? _apiDomain;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public ZohoCrmConnection(IHttpClientFactory httpFactory, ZohoOptions options, ILogger<ZohoCrmConnection> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiClient = httpFactory.CreateClient("zoho-api");
            _tokenClient = httpFactory.CreateClient("zoho-token");
        }

        private bool IsTokenExpired()
        {
            return string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry;
        }

        private async Task RefreshTokenAsync(CancellationToken cancellationToken)
        {
            // Build form
            var form = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", _options.ClientId ?? throw new InvalidOperationException("ClientId not configured")),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret ?? throw new InvalidOperationException("ClientSecret not configured")),
                new KeyValuePair<string, string>("refresh_token", _options.RefreshToken ?? throw new InvalidOperationException("RefreshToken not configured"))
            };

            var req = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl ?? throw new InvalidOperationException("TokenUrl not configured"))
            {
                Content = new FormUrlEncodedContent(form)
            };

            HttpResponseMessage tokenResponse;
            try
            {
                tokenResponse = await _tokenClient.SendAsync(req, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Token refresh failed: request error", ex);
            }

            var body = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Token refresh failed: {tokenResponse.StatusCode} - {body}");
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(body);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Token refresh failed: invalid JSON", ex);
            }

            if (!doc.RootElement.TryGetProperty("access_token", out var atElem) || atElem.GetString() is not string at || string.IsNullOrEmpty(at))
            {
                throw new InvalidOperationException($"Token refresh failed: missing access_token - {body}");
            }

            var accessToken = at;

            int expiresIn = 3600;
            if (doc.RootElement.TryGetProperty("expires_in", out var expElem) && expElem.ValueKind == JsonValueKind.Number && expElem.TryGetInt32(out var parsedExp))
            {
                expiresIn = parsedExp == 0 ? 3600 : parsedExp;
            }

            string? apiDomain = null;
            if (doc.RootElement.TryGetProperty("api_domain", out var apiElem) && apiElem.ValueKind == JsonValueKind.String)
            {
                apiDomain = apiElem.GetString();
            }

            // Apply clock skew/safety
            if (expiresIn < 60)
            {
                // usable now but do not cache
                _accessToken = accessToken;
                _tokenExpiry = DateTime.MinValue;
            }
            else
            {
                _accessToken = accessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 30);
            }

            _apiDomain = string.IsNullOrEmpty(apiDomain) ? _options.BaseUrl : apiDomain.TrimEnd('/');
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
        {
            // Ensure token is present (double-checked locking)
            if (IsTokenExpired())
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    if (IsTokenExpired())
                    {
                        await RefreshTokenAsync(cancellationToken);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            var effectiveBase = !string.IsNullOrEmpty(_apiDomain) ? _apiDomain : _options.BaseUrl ?? throw new InvalidOperationException("BaseUrl not configured");
            var rel = relativePath.StartsWith("/") ? relativePath : "/" + relativePath;
            var targetUri = new Uri(new Uri(effectiveBase), rel);

            var request = new HttpRequestMessage(method, targetUri);
            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            request.Headers.Add("Authorization", $"Zoho-oauthtoken {_accessToken}");

            HttpResponseMessage response;
            try
            {
                response = await _apiClient.SendAsync(request, cancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException("Request timed out", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Network error during request", ex);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Dispose before retry
                response.Dispose();

                // Force refresh token once
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    await RefreshTokenAsync(cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }

                // Build a brand new request per rules
                var retryRequest = new HttpRequestMessage(method, targetUri);
                if (body != null)
                {
                    retryRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                retryRequest.Headers.Add("Authorization", $"Zoho-oauthtoken {_accessToken}");

                HttpResponseMessage retryResponse;
                try
                {
                    retryResponse = await _apiClient.SendAsync(retryRequest, cancellationToken);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Network error during retry request", ex);
                }

                return retryResponse;
            }

            return response;
        }
    }
}
