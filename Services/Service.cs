using System.Net;
using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using Httpcsharplambda.Models;
using Microsoft.Extensions.Logging;

namespace Httpcsharplambda.Services;

public interface ITravelcardDbConnection
{
    Task<HttpResponseMessage> SendAsync(APIGatewayProxyRequest request, CancellationToken cancellationToken);
}

public interface ITravelcardDbService
{
    Task<APIGatewayProxyResponse> ProcessAsync(APIGatewayProxyRequest request, CancellationToken cancellationToken);
}

public class Service : ITravelcardDbConnection, ITravelcardDbService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SecretsHelper _secretsHelper;
    private readonly ILogger<Service> _logger;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
    private readonly string _baseUrl;
    private readonly string _functionKey;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tokenUrl;
    private readonly string _scopes;
    private string _accessToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public Service(IHttpClientFactory httpClientFactory, SecretsHelper secretsHelper, ILogger<Service> logger)
    {
        _httpClientFactory = httpClientFactory;
        _secretsHelper = secretsHelper;
        _logger = logger;
        _baseUrl = (Environment.GetEnvironmentVariable("TRAVELCARD_API_URL") ?? throw new InvalidOperationException("TRAVELCARD_API_URL is not configured")).TrimEnd('/');
        _functionKey = Environment.GetEnvironmentVariable("TRAVELCARD_FUNCTION_KEY") ?? throw new InvalidOperationException("TRAVELCARD_FUNCTION_KEY is not configured");
        var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME") ?? throw new InvalidOperationException("AWS_SECRET_NAME is not configured");
        var secrets = _secretsHelper.GetSecretsAsync(secretName).GetAwaiter().GetResult();
        _clientId = _secretsHelper.ResolveValueAsync(secrets, "AZURE-CLIENT-ID").GetAwaiter().GetResult();
        _clientSecret = _secretsHelper.ResolveValueAsync(secrets, "AZURE-CLIENT-SECRET").GetAwaiter().GetResult();
        _tokenUrl = _secretsHelper.ResolveValueAsync(secrets, "AZURE-TOKEN-URL").GetAwaiter().GetResult();
        _scopes = _secretsHelper.ResolveValueAsync(secrets, "AZURE-SCOPES").GetAwaiter().GetResult();
    }

    public async Task<APIGatewayProxyResponse> ProcessAsync(APIGatewayProxyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.Equals(request.HttpMethod, HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase))
            {
                return BuildResponse((int)HttpStatusCode.MethodNotAllowed, new { error = "Only HTTP POST is supported" });
            }

            var rawBody = request.Body ?? string.Empty;
            ValidateRequest(rawBody);
            using var response = await SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return BuildResponse((int)response.StatusCode, responseBody);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Processing failed");
            return BuildResponse((int)HttpStatusCode.BadGateway, new { error = ex.Message });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout");
            return BuildResponse((int)HttpStatusCode.GatewayTimeout, new { error = "Request timed out" });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error");
            return BuildResponse((int)HttpStatusCode.BadGateway, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled service error");
            return BuildResponse((int)HttpStatusCode.InternalServerError, new { error = ex.Message });
        }
    }

    public async Task<HttpResponseMessage> SendAsync(APIGatewayProxyRequest request, CancellationToken cancellationToken)
    {
        var apiClient = _httpClientFactory.CreateClient("travelcard-api");
        await EnsureTokenAsync(cancellationToken);
        var targetUri = new UriBuilder(new Uri(new Uri(_baseUrl), "/api/travelcard"))
        {
            Query = $"code={Uri.EscapeDataString(_functionKey)}"
        }.Uri;

        var body = request.Body ?? string.Empty;
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetUri);
        httpRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");
        httpRequest.Headers.Add("client_id", GetHeaderValue(request.Headers, "client_id") ?? string.Empty);
        httpRequest.Headers.Add("Authorization", $"Bearer {_accessToken}");
        httpRequest.Headers.TryAddWithoutValidation("Content-Type", "application/json");

        var response = await apiClient.SendAsync(httpRequest, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await ForceRefreshTokenAsync(cancellationToken);
            var retryRequest = new HttpRequestMessage(HttpMethod.Post, targetUri);
            retryRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");
            retryRequest.Headers.Add("client_id", GetHeaderValue(request.Headers, "client_id") ?? string.Empty);
            retryRequest.Headers.Add("Authorization", $"Bearer {_accessToken}");
            retryRequest.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            return await apiClient.SendAsync(retryRequest, cancellationToken);
        }

        if ((int)response.StatusCode >= 400)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("External API failure: status={StatusCode}, body={Body}", (int)response.StatusCode, errorBody);
            throw new InvalidOperationException($"External API failure: {(int)response.StatusCode} - {errorBody}");
        }

        return response;
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

    private async Task RefreshTokenAsync(CancellationToken cancellationToken)
    {
        var tokenClient = _httpClientFactory.CreateClient("travelcard-token");
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["scope"] = _scopes
        };
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _tokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };

        using var tokenResponse = await tokenClient.SendAsync(tokenRequest, cancellationToken);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Token generation failure: status={StatusCode}, body={Body}", (int)tokenResponse.StatusCode, tokenBody);
            throw new InvalidOperationException($"Token refresh failed: {(int)tokenResponse.StatusCode} - {tokenBody}");
        }

        var payload = System.Text.Json.JsonDocument.Parse(tokenBody ?? "{}");
        if (!payload.RootElement.TryGetProperty("access_token", out var accessTokenElement) || string.IsNullOrWhiteSpace(accessTokenElement.GetString()))
        {
            _logger.LogError("Token generation failure: missing access_token. body={Body}", tokenBody);
            throw new InvalidOperationException($"Token refresh failed: {(int)tokenResponse.StatusCode} - {tokenBody}");
        }

        var expiresIn = 3600;
        if (payload.RootElement.TryGetProperty("expires_in", out var expiresInElement) && expiresInElement.TryGetInt32(out var parsedExpiresIn) && parsedExpiresIn > 0)
        {
            expiresIn = parsedExpiresIn;
        }

        _accessToken = accessTokenElement.GetString() ?? throw new InvalidOperationException($"Token refresh failed: {(int)tokenResponse.StatusCode} - {tokenBody}");
        _tokenExpiry = expiresIn < 60 ? DateTime.MinValue : DateTime.UtcNow.AddSeconds(expiresIn - 30);
    }

    private bool IsTokenExpired() => string.IsNullOrWhiteSpace(_accessToken) || DateTime.UtcNow >= _tokenExpiry;

    private void ValidateRequest(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            _logger.LogError("Validation failed: body - request body is required");
            throw new InvalidOperationException("Request body is required");
        }
    }

    private static string? GetHeaderValue(IDictionary<string, string>? headers, string name)
    {
        if (headers == null)
        {
            return null;
        }

        foreach (var entry in headers)
        {
            if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value;
            }
        }

        return null;
    }

    private static APIGatewayProxyResponse BuildResponse(int statusCode, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
            Body = body is string stringBody ? stringBody : System.Text.Json.JsonSerializer.Serialize(body)
        };
    }
}