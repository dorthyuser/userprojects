using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace synctesting1109.Services;

public sealed class ZohoHttpConnectionConnection : IZohoHttpConnectionConnection
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ZohoHttpConnectionConnection> _logger;
    private readonly string _baseUrl;
    private readonly string _tokenUrl;
    private readonly string _clientIdKey;
    private readonly string _clientSecretKey;
    private readonly string _refreshTokenKey;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _refreshToken;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private string? _apiDomain;

    public ZohoHttpConnectionConnection(IHttpClientFactory httpClientFactory, ILogger<ZohoHttpConnectionConnection> logger)
    {
        _logger = logger;
        _logger.LogInformation("[ZohoHttpConnectionConnection] Initialising...");
        _httpClientFactory = httpClientFactory;
        _baseUrl = (Environment.GetEnvironmentVariable("ZOHO_KEY_URL") ?? string.Empty).TrimEnd('/');
        _tokenUrl = Environment.GetEnvironmentVariable("ZOHOTOKENURL") ?? throw new InvalidOperationException("ZOHOTOKENURL not configured");
        _clientIdKey = Environment.GetEnvironmentVariable("ZOHOCLIENTID") ?? string.Empty;
        _clientSecretKey = Environment.GetEnvironmentVariable("ZOHOCLIENTSECRET") ?? string.Empty;
        _refreshTokenKey = Environment.GetEnvironmentVariable("ZOHOREFRESHTOKEN") ?? string.Empty;
        _clientId = SecretHelper.Get(_clientIdKey, "ZOHOCLIENTID");
        _clientSecret = SecretHelper.Get(_clientSecretKey, "ZOHOCLIENTSECRET");
        _refreshToken = SecretHelper.Get(_refreshTokenKey, "ZOHOREFRESHTOKEN");
        _logger.LogInformation("[ZohoHttpConnectionConnection] Init complete — baseUrl={BaseUrlSet}, tokenUrl={TokenUrlSet}, clientId={ClientIdSet}, clientSecret={ClientSecretSet}", !string.IsNullOrWhiteSpace(_baseUrl), !string.IsNullOrWhiteSpace(_tokenUrl), !string.IsNullOrWhiteSpace(_clientId), !string.IsNullOrWhiteSpace(_clientSecret));
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? jsonBody, string? correlationId, CancellationToken cancellationToken = default)
    {
        await EnsureTokenAsync(cancellationToken);
        var request = BuildRequest(method, relativePath, jsonBody, correlationId, _accessToken ?? throw new InvalidOperationException("Access token not available"));
        return await SendInternalAsync(request, method, relativePath, jsonBody, correlationId, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendInternalAsync(HttpRequestMessage request, HttpMethod method, string relativePath, string? jsonBody, string? correlationId, CancellationToken cancellationToken)
    {
        var apiClient = _httpClientFactory.CreateClient("zoho-api");
        try
        {
            _logger.LogInformation("[ZohoHttpConnectionConnection] Sending {Method} request to {Path}", method.Method, relativePath);
            var response = await apiClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogInformation("[ZohoHttpConnectionConnection] Received 401, forcing token refresh and retrying");
                response.Dispose();
                await ForceRefreshTokenAsync(cancellationToken);
                var retryRequest = BuildRequest(method, relativePath, jsonBody, correlationId, _accessToken ?? throw new InvalidOperationException("Access token not available"));
                var retryResponse = await apiClient.SendAsync(retryRequest, cancellationToken);
                _logger.LogInformation("[ZohoHttpConnectionConnection] Request completed with status {StatusCode}", (int)retryResponse.StatusCode);
                return retryResponse;
            }

            _logger.LogInformation("[ZohoHttpConnectionConnection] Request completed with status {StatusCode}", (int)response.StatusCode);
            return response;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "[ZohoHttpConnectionConnection] Request timed out");
            throw new InvalidOperationException("Request timed out", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[ZohoHttpConnectionConnection] Network failure: {Message}", ex.Message);
            throw new InvalidOperationException($"Network failure: {ex.Message}", ex);
        }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string relativePath, string? jsonBody, string? correlationId, string accessToken)
    {
        var effectiveBase = !string.IsNullOrWhiteSpace(_apiDomain) ? _apiDomain : _baseUrl;
        var uri = new Uri(new Uri(effectiveBase), relativePath.StartsWith("/") ? relativePath : "/" + relativePath);
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("Authorization", $"Zoho-oauthtoken {accessToken}");
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.Add("X-Correlation-Id", correlationId);
        }
        if (!string.IsNullOrWhiteSpace(jsonBody))
        {
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private bool IsTokenExpired() => string.IsNullOrWhiteSpace(_accessToken) || DateTime.UtcNow >= _tokenExpiry;

    private async Task EnsureTokenAsync(CancellationToken cancellationToken)
    {
        if (!IsTokenExpired()) return;
        await _tokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!IsTokenExpired()) return;
            await RefreshTokenAsync(cancellationToken);
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    private async Task ForceRefreshTokenAsync(CancellationToken cancellationToken)
    {
        await _tokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            await RefreshTokenAsync(cancellationToken);
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    private async Task RefreshTokenAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[ZohoHttpConnectionConnection] Refreshing access token");
        var tokenClient = _httpClientFactory.CreateClient("zoho-token");
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = _refreshToken,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };

        var response = await tokenClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("[ZohoHttpConnectionConnection] Token refresh failed: {StatusCode}", (int)response.StatusCode);
            throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("access_token", out var accessTokenEl) || string.IsNullOrWhiteSpace(accessTokenEl.GetString()))
        {
            throw new InvalidOperationException("Token refresh failed: missing access_token");
        }

        var accessToken = accessTokenEl.GetString()!;
        var expiresIn = 3600;
        if (doc.RootElement.TryGetProperty("expires_in", out var expiresEl) && expiresEl.TryGetInt32(out var parsedExpires) && parsedExpires > 0)
        {
            expiresIn = parsedExpires;
        }

        var apiDomain = doc.RootElement.TryGetProperty("api_domain", out var apiDomainEl) ? apiDomainEl.GetString() : null;
        _accessToken = accessToken;
        _apiDomain = string.IsNullOrWhiteSpace(apiDomain) ? null : apiDomain.TrimEnd('/');
        _tokenExpiry = DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn - 30));
        _logger.LogInformation("[ZohoHttpConnectionConnection] Token refreshed, expires in {ExpiresIn}s", expiresIn);
    }
}