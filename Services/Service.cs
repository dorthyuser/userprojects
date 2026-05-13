using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Httptravelcardch104Lambda.Models;

namespace Httptravelcardch104Lambda.Services;

public class Service
{
    private static readonly HttpClient _apiClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly HttpClient _tokenClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly string _baseUrl;
    private static readonly string _tokenUrl;
    private static readonly string _clientId;
    private static readonly string _clientSecret;
    private static readonly string _scopes;
    private static readonly string _functionKey;

    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    static Service()
    {
        // Read directly from env — no Secrets Manager lookup
        _baseUrl    = (Environment.GetEnvironmentVariable("TRAVELCARD_API_URL")     ?? throw new InvalidOperationException("Missing env var: TRAVELCARD_API_URL")).TrimEnd('/');
        _functionKey = Environment.GetEnvironmentVariable("TRAVELCARD_FUNCTION_KEY") ?? throw new InvalidOperationException("Missing env var: TRAVELCARD_FUNCTION_KEY");

        // Read from Secrets Manager via xName pattern
        var result  = Task.Run(LoadSecretsAsync).GetAwaiter().GetResult();
        _tokenUrl    = result.TokenUrl;
        _clientId    = result.ClientId;
        _clientSecret = result.ClientSecret;
        _scopes      = result.Scopes;
    }

    public async Task<ServiceResponse> ForwardAsync(string body, IDictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        var uri   = BuildTargetUri();
        var token = await GetValidTokenAsync(cancellationToken);

        using var request = CreateRequest(uri, body, headers, token);
        var response = await _apiClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await ForceRefreshTokenAsync(cancellationToken);
            using var retryRequest = CreateRequest(uri, body, headers, _accessToken ?? throw new InvalidOperationException("Token refresh failed: empty access token"));
            var retryResponse = await _apiClient.SendAsync(retryRequest, cancellationToken);
            return await BuildServiceResponseAsync(retryResponse);
        }

        return await BuildServiceResponseAsync(response);
    }

    private static async Task<SecretsData> LoadSecretsAsync()
    {
        var secretName   = Environment.GetEnvironmentVariable("AWS_SECRET_NAME") ?? throw new InvalidOperationException("Missing env var: AWS_SECRET_NAME");
        var secretClient = new AmazonSecretsManagerClient();
        var response     = await secretClient.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName });

        if (string.IsNullOrWhiteSpace(response.SecretString))
        {
            throw new InvalidOperationException("Secret payload is empty.");
        }

        var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString, _jsonOptions)
                      ?? throw new InvalidOperationException("Secret payload parsing failed.");

        var tokenUrlKey     = Environment.GetEnvironmentVariable("AZURE-TOKEN-URL")      ?? throw new InvalidOperationException("Missing env var: AZURE-TOKEN-URL");
        var clientIdKey     = Environment.GetEnvironmentVariable("AZURE-CLIENT-ID")      ?? throw new InvalidOperationException("Missing env var: AZURE-CLIENT-ID");
        var clientSecretKey = Environment.GetEnvironmentVariable("AZURE-CLIENT-SECRET")  ?? throw new InvalidOperationException("Missing env var: AZURE-CLIENT-SECRET");
        var scopesKey       = Environment.GetEnvironmentVariable("AZURE-SCOPE")          ?? throw new InvalidOperationException("Missing env var: AZURE-SCOPE");

        return new SecretsData(
            secrets[tokenUrlKey],
            secrets[clientIdKey],
            secrets[clientSecretKey],
            secrets[scopesKey]);
    }

    private async Task<string> GetValidTokenAsync(CancellationToken cancellationToken)
    {
        if (!IsTokenExpired())
        {
            return _accessToken!;
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

        return _accessToken!;
    }

    private bool IsTokenExpired() => string.IsNullOrWhiteSpace(_accessToken) || DateTime.UtcNow >= _tokenExpiry;

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
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "client_credentials",
            ["client_id"]     = _clientId,
            ["client_secret"] = _clientSecret,
            ["scope"]         = _scopes
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenUrl)
        {
            Content = content
        };

        var response     = await _tokenClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} - {responseBody}");
        }

        var tokenPayload = JsonSerializer.Deserialize<TokenResponse>(responseBody, _jsonOptions)
                           ?? throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} - {responseBody}");

        if (string.IsNullOrWhiteSpace(tokenPayload.AccessToken))
        {
            throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} - {responseBody}");
        }

        var expiresIn = tokenPayload.ExpiresIn.GetValueOrDefault(3600);
        _accessToken  = tokenPayload.AccessToken;
        _tokenExpiry  = DateTime.UtcNow.AddSeconds(Math.Max(30, expiresIn - 30));
    }

    private HttpRequestMessage CreateRequest(Uri uri, string body, IDictionary<string, string>? headers, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.TryAddWithoutValidation("client_id", GetHeaderValue(headers, "client_id") ?? _clientId);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return request;
    }

    private static Uri BuildTargetUri()
    {
        var separator = _baseUrl.Contains('?') ? "&" : "?";
        return new Uri($"{_baseUrl}{separator}code={Uri.EscapeDataString(_functionKey)}");
    }

    private static string? GetHeaderValue(IDictionary<string, string>? headers, string name)
    {
        if (headers is null)
        {
            return null;
        }

        foreach (var header in headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return header.Value;
            }
        }

        return null;
    }

    private static async Task<ServiceResponse> BuildServiceResponseAsync(HttpResponseMessage response)
    {
        using (response)
        {
            var body = await response.Content.ReadAsStringAsync();
            return new ServiceResponse(response.StatusCode, body, null);
        }
    }

    // Only SM-resolved fields — BaseUrl and FunctionKey come directly from env
    private sealed record SecretsData(string TokenUrl, string ClientId, string ClientSecret, string Scopes);

    private sealed class TokenResponse
    {
        public string? AccessToken { get; set; }
        public int?    ExpiresIn   { get; set; }
    }
}

public sealed record ServiceResponse(HttpStatusCode StatusCode, string Body, string? GeneratedId);
