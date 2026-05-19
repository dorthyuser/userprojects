using System.Net;
using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using LambdacsharphttpLambda.Models;
using Microsoft.Extensions.Logging;

namespace LambdacsharphttpLambda.Services;

public interface ITravelcardDbConnection
{
    Task<HttpResponseMessage> SendAsync(string requestBody, CancellationToken cancellationToken);
}

public interface ITravelcardDbService
{
    Task<APIGatewayProxyResponse> ForwardAsync(APIGatewayProxyRequest request);
}

public sealed class TravelcardService : ITravelcardDbService
{
    private readonly ITravelcardDbConnection _connection;
    private readonly ILogger<TravelcardService> _logger;

    public TravelcardService(ITravelcardDbConnection connection, ILogger<TravelcardService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<APIGatewayProxyResponse> ForwardAsync(APIGatewayProxyRequest request)
    {
        try
        {
            using var downstreamResponse = await _connection.SendAsync(request.Body ?? string.Empty, CancellationToken.None);
            var responseBody = await downstreamResponse.Content.ReadAsStringAsync();
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)downstreamResponse.StatusCode,
                Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Content-Type"] = "application/json"
                },
                Body = responseBody
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "External API failure while forwarding Travelcard request");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadGateway,
                Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Content-Type"] = "application/json"
                },
                Body = System.Text.Json.JsonSerializer.Serialize(new Response
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "DOWNSTREAM_ERROR",
                        Message = "Travelcard API request failed."
                    }
                })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while forwarding Travelcard request");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Content-Type"] = "application/json"
                },
                Body = System.Text.Json.JsonSerializer.Serialize(new Response
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An unexpected error occurred."
                    }
                })
            };
        }
    }
}

public sealed class TravelcardConnection : ITravelcardDbConnection
{
    private static readonly HttpClient _apiClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly HttpClient _tokenClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly ILogger<TravelcardConnection> _logger;
    private readonly SecretsHelper _secretsHelper;
    private readonly string _baseUrl;
    private readonly string _functionKey;
    private readonly string _tokenUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _scopes;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public TravelcardConnection(SecretsHelper secretsHelper, ILogger<TravelcardConnection> logger)
    {
        _secretsHelper = secretsHelper;
        _logger = logger;
        _baseUrl = Environment.GetEnvironmentVariable("TRAVELCARD_API_URL")?.TrimEnd('/') ?? throw new InvalidOperationException("TRAVELCARD_API_URL not configured");
        _functionKey = Environment.GetEnvironmentVariable("TRAVELCARD_FUNCTION_KEY") ?? throw new InvalidOperationException("TRAVELCARD_FUNCTION_KEY not configured");
        _tokenUrl = Environment.GetEnvironmentVariable("AZURE_TOKEN_URL") ?? throw new InvalidOperationException("AZURE_TOKEN_URL not configured");
        var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME") ?? throw new InvalidOperationException("AWS_SECRET_NAME not configured");
        var secrets = _secretsHelper.GetSecretsAsync(secretName).GetAwaiter().GetResult();
        _clientId = _secretsHelper.Resolve(secrets, "AZURE-CLIENT-ID");
        _clientSecret = _secretsHelper.Resolve(secrets, "AZURE-CLIENT-SECRET");
        _scopes = _secretsHelper.Resolve(secrets, "AZURE-SCOPES");
    }

    public async Task<HttpResponseMessage> SendAsync(string requestBody, CancellationToken cancellationToken)
    {
        await EnsureTokenAsync(cancellationToken);
        var targetUri = BuildUriWithFunctionKey("/api/travelcard");
        using var request = BuildRequest(targetUri, requestBody);
        var firstResponse = await _apiClient.SendAsync(request, cancellationToken);
        if ((int)firstResponse.StatusCode == 401)
        {
            firstResponse.Dispose();
            _tokenExpiry = DateTime.MinValue;
            await EnsureTokenAsync(cancellationToken);
            var retryRequest = BuildRequest(targetUri, requestBody);
            return await _apiClient.SendAsync(retryRequest, cancellationToken);
        }

        return firstResponse;
    }

    private HttpRequestMessage BuildRequest(Uri uri, string requestBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("client_id", _clientId);
        request.Headers.Add("Authorization", $"Bearer {_accessToken}");
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        return request;
    }

    private Uri BuildUriWithFunctionKey(string path)
    {
        var uri = new Uri(_baseUrl.EndsWith('/') ? _baseUrl : _baseUrl + "/");
        var target = new Uri(uri, path);
        var builder = new UriBuilder(target)
        {
            Query = $"code={Uri.EscapeDataString(_functionKey)}"
        };
        return builder.Uri;
    }

    private bool IsTokenExpired() => string.IsNullOrWhiteSpace(_accessToken) || DateTime.UtcNow >= _tokenExpiry;

    private async Task EnsureTokenAsync(CancellationToken cancellationToken)
    {
        if (!IsTokenExpired())
        {
            return;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!IsTokenExpired())
            {
                return;
            }

            await RefreshTokenAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RefreshTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
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

            using var response = await _tokenClient.SendAsync(tokenRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} - {body}");
            }

            var token = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} - {body}");
            if (string.IsNullOrWhiteSpace(token.AccessToken))
            {
                throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} - {body}");
            }

            _accessToken = token.AccessToken;
            var expiresIn = token.ExpiresIn <= 0 ? 3600 : token.ExpiresIn;
            _tokenExpiry = expiresIn < 60 ? DateTime.MinValue : DateTime.UtcNow.AddSeconds(expiresIn - 30);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token generation failure for Travelcard connection");
            throw;
        }
    }

    private sealed class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}