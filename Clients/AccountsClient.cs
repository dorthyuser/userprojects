using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace BusTravelAccountsEaMainFunction;

public sealed class AccountsClient
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    private readonly Settings _settings;

    public AccountsClient(Settings settings)
    {
        _settings = settings;
    }

    public async Task<DownstreamResponse> GetAccountsAsync(string email, string correlationId, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, "/accounts", correlationId);
        var uriBuilder = new UriBuilder("https", _settings.AccountsHost, _settings.AccountsPort, CombinePath(_settings.AccountsBasePath, "/accounts"));
        uriBuilder.Query = string.IsNullOrWhiteSpace(email) ? string.Empty : $"email={Uri.EscapeDataString(email)}";
        request.RequestUri = uriBuilder.Uri;
        return await SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DownstreamResponse> GetAccountByIdAsync(string id, string correlationId, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"/accounts/{Uri.EscapeDataString(id)}", correlationId);
        request.RequestUri = new Uri($"https://{_settings.AccountsHost}:{_settings.AccountsPort}{CombinePath(_settings.AccountsBasePath, $"/accounts/{Uri.EscapeDataString(id)}")}");
        return await SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DownstreamResponse> CreateAccountAsync(string body, string correlationId, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "/accounts", correlationId);
        request.RequestUri = new Uri($"https://{_settings.AccountsHost}:{_settings.AccountsPort}{CombinePath(_settings.AccountsBasePath, "/accounts")}");
        request.Content = new StringContent(body ?? string.Empty);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return await SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DownstreamResponse> UpdateAccountAsync(string id, string body, string correlationId, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Put, $"/accounts/{Uri.EscapeDataString(id)}", correlationId);
        request.RequestUri = new Uri($"https://{_settings.AccountsHost}:{_settings.AccountsPort}{CombinePath(_settings.AccountsBasePath, $"/accounts/{Uri.EscapeDataString(id)}")}");
        request.Content = new StringContent(body ?? string.Empty);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return await SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string correlationId)
    {
        var request = new HttpRequestMessage(method, new Uri("http://localhost"));
        request.Headers.TryAddWithoutValidation("CUSTOMER_CORRELATION_ID", correlationId);
        request.Headers.TryAddWithoutValidation("X_CORRELATION_ID", correlationId);
        request.Headers.TryAddWithoutValidation("client_secret", _settings.AccountsClientSecret);
        request.Headers.TryAddWithoutValidation("client_id", _settings.AccountsClientId);
        return request;
    }

    private static async Task<DownstreamResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var body = response.Content is null ? string.Empty : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return new DownstreamResponse
        {
            StatusCode = (int)response.StatusCode,
            Body = body,
            ContentType = response.Content?.Headers.ContentType?.ToString()
        };
    }

    private static string CombinePath(string basePath, string path)
    {
        var left = string.IsNullOrWhiteSpace(basePath) ? string.Empty : basePath.TrimEnd('/');
        var right = path.StartsWith('/') ? path : "/" + path;
        return left + right;
    }
}