using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace synctesting1050.Services;

public sealed class ZohoHttpConnectionConnection : IZohoHttpConnectionConnection
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ZohoHttpConnectionConnection> _logger;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
    private readonly string _baseUrl;
    private readonly string _tokenUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _refreshToken;
    private string? _accessToken;
    private DateTime _tokenExpiryUtc = DateTime.MinValue;
    private string? _apiDomain;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ZohoHttpConnectionConnection(IHttpClientFactory httpClientFactory, ILogger<ZohoHttpConnectionConnection> logger)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _logger.LogInformation("[ZohoHttpConnectionConnection] Initialising...");
        _baseUrl = (Environment.GetEnvironmentVariable("ZOHO_KEY_URL") ?? "").TrimEnd('/');
        _tokenUrl = Environment.GetEnvironmentVariable("ZOHOTOKENURL") ?? throw new InvalidOperationException("ZOHOTOKENURL not configured");
        var clientIdKey = Environment.GetEnvironmentVariable("ZOHOCLIENTID") ?? "";
        var clientSecretKey = Environment.GetEnvironmentVariable("ZOHOCLIENTSECRET") ?? "";
        var refreshTokenKey = Environment.GetEnvironmentVariable("ZOHOREFRESHTOKEN") ?? "";
        if (string.IsNullOrWhiteSpace(clientIdKey)) _logger.LogWarning("[ZohoHttpConnectionConnection] ZOHOCLIENTID env var is empty");
        if (string.IsNullOrWhiteSpace(clientSecretKey)) _logger.LogWarning("[ZohoHttpConnectionConnection] ZOHOCLIENTSECRET env var is empty");
        if (string.IsNullOrWhiteSpace(refreshTokenKey)) _logger.LogWarning("[ZohoHttpConnectionConnection] ZOHOREFRESHTOKEN env var is empty");
        _clientId = SecretHelper.Get(clientIdKey, "ZOHOCLIENTID");
        _clientSecret = SecretHelper.Get(clientSecretKey, "ZOHOCLIENTSECRET");
        _refreshToken = SecretHelper.Get(refreshTokenKey, "ZOHOREFRESHTOKEN");
        _logger.LogInformation("[ZohoHttpConnectionConnection] Init complete — baseUrlConfigured={BaseUrlConfigured}, tokenUrlConfigured={TokenUrlConfigured}, clientIdConfigured={ClientIdConfigured}, clientSecretConfigured={ClientSecretConfigured}", !string.IsNullOrWhiteSpace(_baseUrl), !string.IsNullOrWhiteSpace(_tokenUrl), !string.IsNullOrWhiteSpace(_clientId), !string.IsNullOrWhiteSpace(_clientSecret));
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? queryString, string? bodyJson, string? ifModifiedSince, CancellationToken cancellationToken = default)
    {
        await EnsureTokenAsync(cancellationToken);
        var targetUri = BuildUri(relativePath, queryString);
        _logger.LogInformation("[ZohoHttpConnectionConnection] Sending {Method} request to {Path}", method.Method, relativePath);
        using var request = BuildRequest(method, targetUri, bodyJson, ifModifiedSince, _accessToken ?? throw new InvalidOperationException("Access token missing"));
        var client = _httpClientFactory.CreateClient("zoho-api");
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "[ZohoHttpConnectionConnection] Request timed out");
            throw new InvalidOperationException("Request timed out", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[ZohoHttpConnectionConnection] Network failure: {Message}", ex.Message);
            throw new InvalidOperationException("Network failure", ex);
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("[ZohoHttpConnectionConnection] Received 401, forcing token refresh and retrying");
            response.Dispose();
            await ForceRefreshTokenAsync(cancellationToken);
            using var retryRequest = BuildRequest(method, targetUri, bodyJson, ifModifiedSince, _accessToken ?? throw new InvalidOperationException("Access token missing"));
            var retryResponse = await client.SendAsync(retryRequest, cancellationToken);
            _logger.LogInformation("[ZohoHttpConnectionConnection] Request completed with status {StatusCode}", (int)retryResponse.StatusCode);
            return retryResponse;
        }
        _logger.LogInformation("[ZohoHttpConnectionConnection] Request completed with status {StatusCode}", (int)response.StatusCode);
        return response;
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, Uri targetUri, string? bodyJson, string? ifModifiedSince, string accessToken)
    {
        var request = new HttpRequestMessage(method, targetUri);
        request.Headers.Add("Authorization", $"Zoho-oauthtoken {accessToken}");
        if (!string.IsNullOrWhiteSpace(ifModifiedSince)) request.Headers.Add("If-Modified-Since", ifModifiedSince);
        if (bodyJson is not null) request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        return request;
    }

    private Uri BuildUri(string relativePath, string? queryString)
    {
        var effectiveBase = !string.IsNullOrWhiteSpace(_apiDomain) ? _apiDomain! : _baseUrl;
        var builder = new UriBuilder(new Uri(new Uri(effectiveBase), relativePath.StartsWith('/') ? relativePath : "/" + relativePath));
        if (!string.IsNullOrWhiteSpace(queryString)) builder.Query = queryString.TrimStart('?');
        return builder.Uri;
    }

    private bool IsTokenExpired() => string.IsNullOrWhiteSpace(_accessToken) || DateTime.UtcNow >= _tokenExpiryUtc;

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
        HttpResponseMessage response;
        try
        {
            response = await tokenClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ZohoHttpConnectionConnection] Token refresh failed");
            throw new InvalidOperationException("Token refresh failed", ex);
        }
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} - {body}");
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("access_token", out var at) || string.IsNullOrWhiteSpace(at.GetString()))
            throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} - {body}");
        _accessToken = at.GetString();
        var expiresIn = 3600;
        if (doc.RootElement.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var parsed) && parsed > 0) expiresIn = parsed;
        if (expiresIn < 60) _tokenExpiryUtc = DateTime.MinValue; else _tokenExpiryUtc = DateTime.UtcNow.AddSeconds(expiresIn - 30);
        if (doc.RootElement.TryGetProperty("api_domain", out var ad) && !string.IsNullOrWhiteSpace(ad.GetString())) _apiDomain = ad.GetString();
        _logger.LogInformation("[ZohoHttpConnectionConnection] Token refreshed, expires in {ExpiresIn}s", expiresIn);
    }
}