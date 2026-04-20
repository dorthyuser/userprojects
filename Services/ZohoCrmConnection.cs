using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using ZohoProject3.Models;

namespace ZohoProject3.Services
{
    public class ZohoCrmConnection : IZohoCrmConnection
    {
        private readonly HttpClient _apiClient;
        private readonly HttpClient _tokenClient;
        private readonly ZohoOptions _options;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private string? _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private string? _apiDomain;

        public ZohoCrmConnection(IHttpClientFactory factory, ZohoOptions options)
        {
            _apiClient = factory.CreateClient("zoho-api-client");
            _tokenClient = factory.CreateClient("zoho-token-client");
            _options = options ?? throw new InvalidOperationException("ZohoOptions not configured");
        }

        private bool IsTokenExpired()
        {
            return string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry;
        }

        private async Task RefreshTokenAsync(CancellationToken cancellationToken)
        {
            var form = new[] {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
                new KeyValuePair<string, string>("refresh_token", _options.RefreshToken)
            };

            var req = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
            {
                Content = new FormUrlEncodedContent(form)
            };

            HttpResponseMessage resp;
            try
            {
                resp = await _tokenClient.SendAsync(req, cancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException("Token request timed out", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Token request failed", ex);
            }

            string body = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Token refresh failed: {((int)resp.StatusCode)} - {body}");
            }

            ZohoTokenResponse? tokenResp = null;
            try
            {
                tokenResp = JsonSerializer.Deserialize<ZohoTokenResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Token refresh failed: invalid JSON", ex);
            }

            if (tokenResp == null || string.IsNullOrEmpty(tokenResp.AccessToken))
            {
                throw new InvalidOperationException($"Token refresh failed: {(int)resp.StatusCode} - {body}");
            }

            var expiresIn = tokenResp.ExpiresIn.GetValueOrDefault(3600);

            if (expiresIn <= 0)
            {
                expiresIn = 3600;
            }

            // Apply clock skew safety
            if (expiresIn < 60)
            {
                // usable now but force next fetch
                _tokenExpiry = DateTime.MinValue;
            }
            else
            {
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 30);
            }

            _accessToken = tokenResp.AccessToken;

            if (!string.IsNullOrEmpty(tokenResp.ApiDomain))
            {
                _apiDomain = tokenResp.ApiDomain.TrimEnd('/');
            }
        }

        private async Task EnsureValidTokenAsync(CancellationToken cancellationToken)
        {
            if (!IsTokenExpired()) return;

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!IsTokenExpired()) return;
                await RefreshTokenAsync(cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? jsonBody, CancellationToken cancellationToken)
        {
            await EnsureValidTokenAsync(cancellationToken);

            var effectiveBase = !string.IsNullOrEmpty(_apiDomain) ? _apiDomain : _options.BaseUrl;
            var relative = relativePath.StartsWith("/") ? relativePath : "/" + relativePath;
            var targetUri = new Uri(new Uri(effectiveBase), relative);

            var request = new HttpRequestMessage(method, targetUri);
            if (!string.IsNullOrEmpty(jsonBody))
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
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
                throw new InvalidOperationException("Network error while sending request", ex);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                response.Dispose();

                // Force refresh and retry once
                // Invalidate token then refresh
                _tokenExpiry = DateTime.MinValue;
                await EnsureValidTokenAsync(cancellationToken);

                var retryRequest = new HttpRequestMessage(method, targetUri);
                if (!string.IsNullOrEmpty(jsonBody))
                {
                    retryRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                retryRequest.Headers.Add("Authorization", $"Zoho-oauthtoken {_accessToken}");

                HttpResponseMessage retryResponse;
                try
                {
                    retryResponse = await _apiClient.SendAsync(retryRequest, cancellationToken);
                }
                catch (TaskCanceledException ex)
                {
                    throw new InvalidOperationException("Retry request timed out", ex);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Network error during retry", ex);
                }

                return retryResponse;
            }

            return response;
        }
    }
}
