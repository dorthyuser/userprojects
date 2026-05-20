using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace zohotesting.Services;

public sealed class ZohoHttpConnectionConnection : IZohoHttpConnectionConnection
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ZohoHttpConnectionConnection> _logger;
    private readonly string _baseUrl;
    private readonly string _tokenUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _refreshToken;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly HttpClient _apiClient;
    private readonly HttpClient _tokenClient;
    private string _accessToken = "";
    private DateTime _tokenExpiry = DateTime.MinValue;
    private string? _apiDomain;

    public ZohoHttpConnectionConnection(IHttpClientFactory httpClientFactory, ILogger<ZohoHttpConnectionConnection> logger)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _logger.LogInformation("[ZohoHttpConnectionConnection] Initialising...");
        _baseUrl = (Environment.GetEnvironmentVariable("ZOHO_KEY_URL") ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(_baseUrl)) _logger.LogWarning("[ZohoHttpConnectionConnection] ZOHO_KEY_URL env var is empty");
        _tokenUrl = Environment.GetEnvironmentVariable("ZOHOTOKENURL") ?? "";
        if (string.IsNullOrWhiteSpace(_tokenUrl)) _logger.LogWarning("[ZohoHttpConnectionConnection] ZOHOTOKENURL env var is empty");
        _clientId = SecretHelper.Get("ZOHOCLIENTID", "ZOHOCLIENTID");
        _clientSecret = SecretHelper.Get("ZOHOCLIENTSECRET", "ZOHOCLIENTSECRET");
        _refreshToken = SecretHelper.Get("ZOHOREFRESHTOKEN", "ZOHOREFRESHTOKEN");
        _apiClient = _httpClientFactory.CreateClient("zoho-api");
        _tokenClient = _httpClientFactory.CreateClient("zoho-token");
        _logger.LogInformation("[ZohoHttpConnectionConnection] Init complete — baseUrl={BaseUrl}, tokenUrl={TokenUrl}, clientId={ClientId}, clientSecret={ClientSecret}", !string.IsNullOrWhiteSpace(_baseUrl), !string.IsNullOrWhiteSpace(_tokenUrl), !string.IsNullOrWhiteSpace(_clientId), !string.IsNullOrWhiteSpace(_clientSecret));
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, HttpContent? content, Dictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        await EnsureTokenAsync(cancellationToken);
        var response = await SendInternalAsync(method, relativePath, content, headers, cancellationToken, false);
        return response;
    }

    public async Task<HttpResponseMessage> SendToTokenEndpointAsync(CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = _refreshToken
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };
        return await _tokenClient.SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendInternalAsync(HttpMethod method, string relativePath, HttpContent? content, Dictionary<string, string>? headers, CancellationToken cancellationToken, bool retried)
    {
        try
        {
            var targetUri = new Uri(new Uri(GetEffectiveBaseUrl()), relativePath.StartsWith("/") ? relativePath : "/" + relativePath);
            _logger.LogInformation("[ZohoHttpConnectionConnection] Sending {METHOD} request to {Path}", method.Method, relativePath);
            using var request = new HttpRequestMessage(method, targetUri);
            request.Headers.Add("Authorization", $"Zoho-oauthtoken {_accessToken}");
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            if (content != null)
            {
                var bytes = await content.ReadAsByteArrayAsync(cancellationToken);
                request.Content = new ByteArrayContent(bytes);
                if (content.Headers.ContentType != null) request.Content.Headers.ContentType = content.Headers.ContentType;
            }
            var response = await _apiClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized && !retried)
            {
                _logger.LogWarning("[ZohoHttpConnectionConnection] Received 401, forcing token refresh and retrying");
                response.Dispose();
                await ForceRefreshTokenAsync(cancellationToken);
                return await SendInternalAsync(method, relativePath, content, headers, cancellationToken, true);
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
            throw;
        }
    }

    private async Task EnsureTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && DateTime.UtcNow < _tokenExpiry) return;
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) && DateTime.UtcNow < _tokenExpiry) return;
            await RefreshTokenAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ForceRefreshTokenAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await RefreshTokenAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RefreshTokenAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[ZohoHttpConnectionConnection] Refreshing access token");
        using var response = await SendToTokenEndpointAsync(cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} - {body}");
        var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("access_token", out var accessTokenEl) || string.IsNullOrWhiteSpace(accessTokenEl.GetString())) throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} - {body}");
        _accessToken = accessTokenEl.GetString()!;
        var expiresIn = 3600;
        if (doc.RootElement.TryGetProperty("expires_in", out var expiresEl) && expiresEl.TryGetInt32(out var parsed) && parsed > 0) expiresIn = parsed;
        if (expiresIn < 60) _tokenExpiry = DateTime.MinValue; else _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 30);
        if (doc.RootElement.TryGetProperty("api_domain", out var apiDomainEl)) _apiDomain = apiDomainEl.GetString();
        _logger.LogInformation("[ZohoHttpConnectionConnection] Token refreshed, expires in {ExpiresIn}s", expiresIn);
    }

    private string GetEffectiveBaseUrl() => !string.IsNullOrWhiteSpace(_apiDomain) ? _apiDomain!.TrimEnd('/') : _baseUrl;
}