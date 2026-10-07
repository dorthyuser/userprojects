using System.Text.Json;

namespace MulecombineMainLambda;

public sealed class EaApiService
{
    private static readonly PaApiService PaApiService = new();

    public async Task<EaApiResult> GetAliveAsync()
    {
        return await Task.FromResult(new EaApiResult
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new EaHealthResponse { Status = "UP" }, EaJsonOptions.Create())
        });
    }

    public async Task<EaApiResult> GetReadyAsync()
    {
        return await Task.FromResult(new EaApiResult
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new EaHealthResponse { Status = "UP" }, EaJsonOptions.Create())
        });
    }

    public async Task<EaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await PaApiService.GetAccountsAsync(query, headers);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<EaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await PaApiService.GetAccountByIdAsync(id, query, headers);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<EaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? headers)
    {
        var result = await PaApiService.PostAccountsAsync(body, headers);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<EaApiResult> PutAccountAsync(string id, string? body, IDictionary<string, string>? headers)
    {
        var result = await PaApiService.PutAccountAsync(id, body, headers);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public EaApiResult BuildUnexpectedError(Exception exception)
    {
        return new EaApiResult
        {
            StatusCode = 500,
            Body = JsonSerializer.Serialize(new EaErrorResponse
            {
                Error = new EaErrorBody
                {
                    ErrorCode = 500,
                    ErrorDateTime = DateTimeOffset.UtcNow,
                    ErrorMessage = $"{exception.GetType().Namespace} {exception.GetType().Name} ERROR",
                    ErrorDescription = "check payload existence or method/payload combination requirement"
                }
            }, EaJsonOptions.Create()),
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = "application/json"
            }
        };
    }
}