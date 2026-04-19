using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using ZohoProject1.Models;

namespace ZohoProject1.Services
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

        public ZohoCrmConnection(System.Net.Http.IHttpClientFactory httpFactory, ZohoOptions options, ILogger<ZohoCrmConnection> logger)
        {
            _options = options ?? throw new InvalidOperationException("Zoho options required");
            _logger = logger;
            _apiClient = httpFactory.CreateClient("zoho-api");
            _tokenClient = httpFactory.CreateClient("zoho-token");
        }

        private bool IsTokenExpired()
        {
            return string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry;
        }

        private async Task RefreshTokenAsync(CancellationToken cancellationToken)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl);
            var form = new []
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
                new KeyValuePair<string, string>("refresh_token", _options.RefreshToken)
            };

            req.Content = new FormUrlEncodedContent(form);

            HttpResponseMessage resp;
            try
            {
                resp = await _tokenClient.SendAsync(req, cancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException("Token refresh request timed out", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Token refresh request failed", ex);
            }

            string body = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Token refresh failed: {resp.StatusCode} - {body}");
            }

            ZohoTokenResponse? tokenResp;
            try
            {
                tokenResp = JsonSerializer.Deserialize<ZohoTokenResponse>(body);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Token refresh failed to parse JSON", ex);
            }

            if (string.IsNullOrEmpty(tokenResp?.AccessToken))
            {
                throw new InvalidOperationException($"Token refresh failed: {resp.StatusCode} - {body}");
            }

            _accessToken = tokenResp.AccessToken;

            var expiresIn = tokenResp.ExpiresIn ?? 3600;

            if (expiresIn < 60)
            {
                // usable for this request, but do not cache
                _tokenExpiry = DateTime.MinValue;
            }
            else
            {
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 30);
            }
        }

        private async Task EnsureTokenAsync(CancellationToken cancellationToken)
        {
            if (!IsTokenExpired())
                return;

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!IsTokenExpired())
                    return;

                await RefreshTokenAsync(cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
        {
            await EnsureTokenAsync(cancellationToken);

            var requestUri = new Uri(_apiClient.BaseAddress!, relativePath.StartsWith("/") ? relativePath : "/" + relativePath);

            var request = new HttpRequestMessage(method, requestUri);
            request.Headers.Add("Authorization", $"Zoho-oauthtoken {_accessToken}");

            string? originalBody = body;
            if (originalBody != null)
            {
                request.Content = new StringContent(originalBody, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response;
            try
            {
                response = await _apiClient.SendAsync(request, cancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException("API request timed out", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("API request failed", ex);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();

                // Force refresh, protect with semaphore to avoid duplicate refreshes
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    await RefreshTokenAsync(cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }

                var retryRequest = new HttpRequestMessage(method, requestUri);
                retryRequest.Headers.Add("Authorization", $"Zoho-oauthtoken {_accessToken}");
                if (originalBody != null)
                {
                    retryRequest.Content = new StringContent(originalBody, Encoding.UTF8, "application/json");
                }

                try
                {
                    var retryResponse = await _apiClient.SendAsync(retryRequest, cancellationToken);
                    return retryResponse;
                }
                catch (TaskCanceledException ex)
                {
                    throw new InvalidOperationException("API retry request timed out", ex);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("API retry request failed", ex);
                }
            }

            return response;
        }
    }
}
