using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TravelcardDb.Models;

namespace TravelcardDb.Services;

public sealed class TravelcardDbConnection : ITravelcardDbConnection
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TravelcardDbConnection> _logger;
    private readonly string _baseUrl;
    private readonly string _tokenUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _scopes;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
    private string _accessToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public TravelcardDbConnection(IHttpClientFactory httpClientFactory, ILogger<TravelcardDbConnection> logger)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _logger.LogInformation("[TravelcardDbConnection] Initialising...");

        var baseUrlValue = Environment.GetEnvironmentVariable("TRAVELCARD_API_URL");
        if (string.IsNullOrWhiteSpace(baseUrlValue))
        {
            _logger.LogWarning("[TravelcardDbConnection] TRAVELCARD_API_URL env var is empty");
        }
        _baseUrl = (baseUrlValue ?? string.Empty).TrimEnd('/');

        var tokenUrlValue = Environment.GetEnvironmentVariable("AZURETOKENURL");
        if (string.IsNullOrWhiteSpace(tokenUrlValue))
        {
            _logger.LogWarning("[TravelcardDbConnection] AZURETOKENURL env var is empty");
        }
        _tokenUrl = tokenUrlValue ?? string.Empty;

        var clientIdKey = Environment.GetEnvironmentVariable("AZURECLIENTID");
        if (string.IsNullOrWhiteSpace(clientIdKey))
        {
            _logger.LogWarning("[TravelcardDbConnection] AZURECLIENTID env var is empty");
        }
        _clientId = SecretHelper.Get(clientIdKey ?? string.Empty, "AZURECLIENTID");

        var clientSecretKey = Environment.GetEnvironmentVariable("AZURECLIENTSECRET");
        if (string.IsNullOrWhiteSpace(clientSecretKey))
        {
            _logger.LogWarning("[TravelcardDbConnection] AZURECLIENTSECRET env var is empty");
        }
        _clientSecret = SecretHelper.Get(clientSecretKey ?? string.Empty, "AZURECLIENTSECRET");

        var scopesKey = Environment.GetEnvironmentVariable("AZURESCOPES");
        if (string.IsNullOrWhiteSpace(scopesKey))
        {
            _logger.LogWarning("[TravelcardDbConnection] AZURESCOPES env var is empty");
        }
        _scopes = SecretHelper.Get(scopesKey ?? string.Empty, "AZURESCOPES");

        _logger.LogInformation("[TravelcardDbConnection] Init complete — baseUrl={BaseUrlSet}, tokenUrl={TokenUrlSet}, clientId={ClientIdSet}, clientSecret={ClientSecretSet}", !string.IsNullOrWhiteSpace(_baseUrl), !string.IsNullOrWhiteSpace(_tokenUrl), !string.IsNullOrWhiteSpace(_clientId), !string.IsNullOrWhiteSpace(_clientSecret));
    }

    public async Task<TravelcardForwardResult> ForwardAsync(string requestBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[TravelcardDbConnection] Sending POST request to /api/travelcard");
        await EnsureTokenAsync(cancellationToken);
        var targetUri = new UriBuilder(new Uri(_baseUrl.EndsWith("/") ? _baseUrl : _baseUrl + "/")) { Path = "api/travelcard", Query = $"code={Uri.EscapeDataString(Environment.GetEnvironmentVariable("TRAVELCARD_FUNCTION_KEY") ?? string.Empty)}" }.Uri;
        var client = _httpClientFactory.CreateClient("api");
        using var request = new HttpRequestMessage(HttpMethod.Post, targetUri);
        request.Headers.Add("client_id", _clientId);
        request.Headers.Add("Authorization", $"Bearer {_accessToken}");
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogInformation("[TravelcardDbConnection] Received 401, forcing token refresh and retrying");
                response.Dispose();
                await ForceRefreshTokenAsync(cancellationToken);
                using var retryRequest = new HttpRequestMessage(HttpMethod.Post, targetUri);
                retryRequest.Headers.Add("client_id", _clientId);
                retryRequest.Headers.Add("Authorization", $"Bearer {_accessToken}");
                retryRequest.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                using var retryResponse = await client.SendAsync(retryRequest, cancellationToken);
                return await ReadResultAsync(retryResponse);
            }

            return await ReadResultAsync(response);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "[TravelcardDbConnection] Request timed out");
            throw new InvalidOperationException("Request timed out", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[TravelcardDbConnection] Network failure: {Message}", ex.Message);
            throw new InvalidOperationException("Network failure", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TravelcardDbConnection] Unexpected failure: {Message}", ex.Message);
            throw;
        }
    }

    private async Task EnsureTokenAsync(CancellationToken cancellationToken)
    {
        if (!IsTokenExpired())
        {
            return;
        }

        await _tokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (IsTokenExpired())
            {
                await RefreshTokenAsync(cancellationToken);
            }
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

    private bool IsTokenExpired()
    {
        return string.IsNullOrWhiteSpace(_accessToken) || DateTime.UtcNow >= _tokenExpiry;
    }

    private async Task RefreshTokenAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[TravelcardDbConnection] Refreshing access token");
        var tokenClient = _httpClientFactory.CreateClient("token");
        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenUrl);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("scope", _scopes)
        });

        using var response = await tokenClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("[TravelcardDbConnection] Token refresh failed: {StatusCode} — {ErrorMessage}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} - {body}");
        }

        var json = JsonDocument.Parse(body);
        if (!json.RootElement.TryGetProperty("access_token", out var tokenElement) || tokenElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(tokenElement.GetString()))
        {
            throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} - {body}");
        }

        var expiresIn = 3600;
        if (json.RootElement.TryGetProperty("expires_in", out var expiresElement) && expiresElement.TryGetInt32(out var parsedExpires) && parsedExpires > 0)
        {
            expiresIn = parsedExpires;
        }

        _accessToken = tokenElement.GetString() ?? string.Empty;
        _tokenExpiry = expiresIn < 60 ? DateTime.MinValue : DateTime.UtcNow.AddSeconds(expiresIn - 30);
        _logger.LogInformation("[TravelcardDbConnection] Token refreshed, expires in {ExpiresIn}s", expiresIn);
    }

    private static async Task<TravelcardForwardResult> ReadResultAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString();
        return new TravelcardForwardResult
        {
            StatusCode = (int)response.StatusCode,
            Body = body,
            ContentType = contentType
        };
    }
}