using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using ZohoProject2.Models;

namespace ZohoProject2.Services
{
    public class ZohoCrmConnection : IZohoCrmConnection
    {
        private readonly HttpClient _apiClient;
        private readonly HttpClient _tokenClient;
        private readonly ZohoOptions _options;
        private readonly ILogger<ZohoCrmConnection> _logger;

        private string? _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public ZohoCrmConnection(IHttpClientFactory httpFactory, ZohoOptions options, ILogger<ZohoCrmConnection> logger)
        {
            _apiClient = httpFactory.CreateClient("zoho_api");
            _tokenClient = httpFactory.CreateClient("zoho_token");
            _options = options ?? throw new InvalidOperationException("ZohoOptions not provided");
            _logger = logger;
        }

        private bool IsTokenExpired()
        {
            if (string.IsNullOrEmpty(_accessToken))
                return true;
            return DateTime.UtcNow >= _tokenExpiry;
        }

        private async Task RefreshTokenAsync(CancellationToken cancellationToken)
        {
            try
            {
                var form = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("client_id", _options.ClientId),
                    new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
                    new KeyValuePair<string, string>("refresh_token", _options.RefreshToken)
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl);
                req.Content = new FormUrlEncodedContent(form);

                using var resp = await _tokenClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
                var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Token refresh failed: {(int)resp.StatusCode} - {body}");
                }

                ZohoTokenResponse? tokenResp;
                try
                {
                    tokenResp = JsonSerializer.Deserialize<ZohoTokenResponse>(body);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException("Token refresh failed: invalid json", ex);
                }

                if (tokenResp == null || string.IsNullOrEmpty(tokenResp.AccessToken))
                {
                    throw new InvalidOperationException($"Token refresh failed: {(int)resp.StatusCode} - {body}");
                }

                _accessToken = tokenResp.AccessToken;

                var expiresIn = tokenResp.ExpiresIn.GetValueOrDefault();
                if (expiresIn == 0)
                {
                    expiresIn = 3600;
                }

                if (expiresIn < 60)
                {
                    // usable now, but do not cache
                    _tokenExpiry = DateTime.MinValue;
                }
                else
                {
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 30);
                }
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException("Token refresh failed", ex);
            }
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
        {
            // Ensure token is present (double-checked locking)
            if (IsTokenExpired())
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (IsTokenExpired())
                    {
                        await RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            var requestUri = BuildUri(relativePath);
            var originalJson = body;

            using var request = new HttpRequestMessage(method, requestUri);
            request.Headers.Add("Authorization", $"Zoho-oauthtoken {_accessToken}");

            if (originalJson != null)
            {
                request.Content = new StringContent(originalJson, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response;
            try
            {
                response = await _apiClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException("Request timed out or was cancelled", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Network error while calling Zoho API", ex);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Dispose original response before retry
                response.Dispose();

                // Force refresh and retry once
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    // re-check inside lock
                    if (IsTokenExpired())
                    {
                        await RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Token wasn't expired according to local state, still force refresh to follow rule
                        await RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                // Build a brand new request for retry
                var retryRequest = new HttpRequestMessage(method, requestUri);
                retryRequest.Headers.Add("Authorization", $"Zoho-oauthtoken {_accessToken}");
                if (originalJson != null)
                {
                    retryRequest.Content = new StringContent(originalJson, Encoding.UTF8, "application/json");
                }

                try
                {
                    var retryResponse = await _apiClient.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
                    return retryResponse;
                }
                catch (TaskCanceledException ex)
                {
                    throw new InvalidOperationException("Request timed out or was cancelled on retry", ex);
                }
                catch (HttpRequestException ex)
                {
                    throw new InvalidOperationException("Network error while calling Zoho API on retry", ex);
                }
            }

            return response;
        }

        private Uri BuildUri(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("relativePath must be provided", nameof(relativePath));

            var path = relativePath.StartsWith("/") ? relativePath : "/" + relativePath;
            var baseUri = new Uri(_options.BaseUrl);
            return new Uri(baseUri, path);
        }
    }
}
