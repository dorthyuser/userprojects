using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;
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

    private static string _baseUrl = string.Empty;
    private static string _tokenUrl = string.Empty;
    private static string _clientId = string.Empty;
    private static string _clientSecret = string.Empty;
    private static string _scopes = string.Empty;
    private static string _functionKey = string.Empty;

    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    static Service()
    {
        try
        {
            LambdaLogger.Log("[INIT] Service static constructor started.");

            // --- Direct env reads (lightweight) ---
            var baseUrlRaw = Environment.GetEnvironmentVariable("TRAVELCARD_API_URL");
            LambdaLogger.Log($"[INIT] TRAVELCARD_API_URL = {(string.IsNullOrWhiteSpace(baseUrlRaw) ? "❌ NOT SET" : "✅ set")}");
            _baseUrl = (baseUrlRaw ?? string.Empty).TrimEnd('/');

            var functionKeyRaw = Environment.GetEnvironmentVariable("TRAVELCARD_FUNCTION_KEY");
            LambdaLogger.Log($"[INIT] TRAVELCARD_FUNCTION_KEY = {(string.IsNullOrWhiteSpace(functionKeyRaw) ? "❌ NOT SET" : "✅ set")}");
            _functionKey = functionKeyRaw ?? string.Empty;

            // NOTE: Do NOT call network operations from static constructor. Secrets will be loaded lazily when needed.
            LambdaLogger.Log("[INIT] Service static constructor completed successfully (secrets will be loaded lazily).");
        }
        catch (Exception ex)
        {
            LambdaLogger.Log($"[INIT] ❌ FATAL — static constructor failed: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
                LambdaLogger.Log($"[INIT] ❌ Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            throw;
        }
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
        // --- AWS_SECRET_NAME ---
        var secretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
        LambdaLogger.Log($"[SM] AWS_SECRET_NAME = {(string.IsNullOrWhiteSpace(secretName) ? "❌ NOT SET" : $"✅ set → value: '{secretName}'")}");
        if (string.IsNullOrWhiteSpace(secretName))
            throw new InvalidOperationException("Missing env var: AWS_SECRET_NAME");

        // --- Fetch from Secrets Manager ---
        LambdaLogger.Log($"[SM] Calling GetSecretValueAsync for secret: '{secretName}'...");
        var secretClient = new AmazonSecretsManagerClient();
        var response     = await secretClient.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName });
        LambdaLogger.Log("[SM] GetSecretValueAsync returned successfully.");

        if (string.IsNullOrWhiteSpace(response.SecretString))
            throw new InvalidOperationException("Secret payload is empty.");

        var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString, _jsonOptions)
                      ?? throw new InvalidOperationException("Secret payload parsing failed.");

        LambdaLogger.Log($"[SM] Secret JSON parsed. Total keys in secret: {secrets.Count}");

        // --- xName lookups: log env var name → key name → exists in secret ---
        var tokenUrlKey = Environment.GetEnvironmentVariable("AZURE_TOKEN_URL");
        LambdaLogger.Log($"[SM] AZURE_TOKEN_URL env = {(string.IsNullOrWhiteSpace(tokenUrlKey) ? "❌ NOT SET" : $"✅ set → key name: '{tokenUrlKey}', exists in secret: {secrets.ContainsKey(tokenUrlKey!)}")}");
        if (string.IsNullOrWhiteSpace(tokenUrlKey)) throw new InvalidOperationException("Missing env var: AZURE_TOKEN_URL");

        var clientIdKey = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        LambdaLogger.Log($"[SM] AZURE_CLIENT_ID env = {(string.IsNullOrWhiteSpace(clientIdKey) ? "❌ NOT SET" : $"✅ set → key name: '{clientIdKey}', exists in secret: {secrets.ContainsKey(clientIdKey!)}")}");
        if (string.IsNullOrWhiteSpace(clientIdKey)) throw new InvalidOperationException("Missing env var: AZURE_CLIENT_ID");

        var clientSecretKey = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
        LambdaLogger.Log($"[SM] AZURE_CLIENT_SECRET env = {(string.IsNullOrWhiteSpace(clientSecretKey) ? "❌ NOT SET" : $"✅ set → key name: '{clientSecretKey}', exists in secret: {secrets.ContainsKey(clientSecretKey!)}")}");
        if (string.IsNullOrWhiteSpace(clientSecretKey)) throw new InvalidOperationException("Missing env var: AZURE_CLIENT_SECRET");

        var scopesKey = Environment.GetEnvironmentVariable("AZURE_SCOPE");
        LambdaLogger.Log($"[SM] AZURE_SCOPE env = {(string.IsNullOrWhiteSpace(scopesKey) ? "❌ NOT SET" : $"✅ set → key name: '{scopesKey}', exists in secret: {secrets.ContainsKey(scopesKey!)}")}");
        if (string.IsNullOrWhiteSpace(scopesKey)) throw new InvalidOperationException("Missing env var: AZURE_SCOPE");

        // --- Resolve values ---
        if (!secrets.TryGetValue(tokenUrlKey, out var tokenUrl))
            throw new InvalidOperationException($"Key '{tokenUrlKey}' (from AZURE_TOKEN_URL) not found in secret.");
        if (!secrets.TryGetValue(clientIdKey, out var clientId))
            throw new InvalidOperationException($"Key '{clientIdKey}' (from AZURE_CLIENT_ID) not found in secret.");
        if (!secrets.TryGetValue(clientSecretKey, out var clientSecret))
            throw new InvalidOperationException($"Key '{clientSecretKey}' (from AZURE_CLIENT_SECRET) not found in secret.");
        if (!secrets.TryGetValue(scopesKey, out var scopes))
            throw new InvalidOperationException($"Key '{scopesKey}' (from AZURE_SCOPE) not found in secret.");

        LambdaLogger.Log("[SM] All 4 secret values resolved successfully ✅");

        return new SecretsData(tokenUrl, clientId, clientSecret, scopes);
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
        // Ensure secrets are loaded before attempting to refresh the token.
        if (string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret) || string.IsNullOrWhiteSpace(_tokenUrl) || string.IsNullOrWhiteSpace(_scopes))
        {
            var secrets = await LoadSecretsAsync();
            _tokenUrl = secrets.TokenUrl;
            _clientId = secrets.ClientId;
            _clientSecret = secrets.ClientSecret;
            _scopes = secrets.Scopes;
        }

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

    private sealed record SecretsData(string TokenUrl, string ClientId, string ClientSecret, string Scopes);

    private sealed class TokenResponse
    {
        public string? AccessToken { get; set; }
        public int?    ExpiresIn   { get; set; }
    }
}

public sealed record ServiceResponse(HttpStatusCode StatusCode, string Body, string? GeneratedId);
