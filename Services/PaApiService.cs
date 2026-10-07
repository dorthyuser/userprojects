using System.Text.Json;

namespace MulecombineMainLambda;

public sealed class PaApiService
{
    private static readonly SaApiService SaApiService = new();
    private static readonly JsonSerializerOptions JsonOptions = PaJsonOptions.Create();

    public async Task<PaApiResult> GetAliveAsync()
    {
        return await Task.FromResult(new PaApiResult
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new PaHealthResponse { Status = "UP" }, JsonOptions),
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        });
    }

    public async Task<PaApiResult> GetReadyAsync()
    {
        return await Task.FromResult(new PaApiResult
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new PaHealthResponse { Status = "UP" }, JsonOptions),
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        });
    }

    public async Task<PaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await SaApiService.GetAccountsAsync(query, headers);
        return Copy(result);
    }

    public async Task<PaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await SaApiService.GetAccountByIdAsync(id, query, headers);
        return Copy(result);
    }

    public async Task<PaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? headers)
    {
        var request = JsonSerializer.Deserialize<PaAccountRequest>(body ?? string.Empty, JsonOptions);
        if (request is null)
        {
            return BuildBadRequest("BAD REQUEST", "check payload existence or method/payload combination requirement");
        }

        var email = request.PersonEmail?.Trim().ToLowerInvariant() ?? string.Empty;
        var existing = await SaApiService.GetAccountsAsync(new Dictionary<string, string> { ["email"] = email }, headers);
        if (existing.StatusCode == 200)
        {
            var accounts = JsonSerializer.Deserialize<List<PaAccountResponse>>(existing.Body ?? "[]", JsonOptions) ?? new List<PaAccountResponse>();
            if (accounts.Count > 0)
            {
                return BuildConflict();
            }
        }
        else if (existing.StatusCode != 204)
        {
            return Copy(existing);
        }

        var create = await SaApiService.PostAccountsAsync(body, headers);
        return Copy(create);
    }

    public async Task<PaApiResult> PutAccountAsync(string id, string? body, IDictionary<string, string>? headers)
    {
        var existing = await SaApiService.GetAccountByIdAsync(id, null, headers);
        if (existing.StatusCode == 204)
        {
            return BuildNotFound();
        }

        if (existing.StatusCode == 200)
        {
            var account = JsonSerializer.Deserialize<PaAccountResponse>(existing.Body ?? string.Empty, JsonOptions);
            if (account?.Hotlisted == true)
            {
                return BuildHotlisted();
            }
        }
        else
        {
            return Copy(existing);
        }

        var update = await SaApiService.PutAccountAsync(id, body, headers);
        return Copy(update);
    }

    private static PaApiResult Copy(SaApiResult result) => new()
    {
        StatusCode = result.StatusCode,
        Body = result.Body,
        Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase)
    };

    private static PaApiResult BuildConflict() => new()
    {
        StatusCode = 409,
        Body = JsonSerializer.Serialize(new PaErrorResponse
        {
            Error = new PaErrorBody
            {
                ErrorCode = 409,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = "CONFLICT",
                ErrorDescription = "An account with this email already exists"
            }
        }, JsonOptions),
        Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    };

    private static PaApiResult BuildNotFound() => new()
    {
        StatusCode = 404,
        Body = JsonSerializer.Serialize(new PaErrorResponse
        {
            Error = new PaErrorBody
            {
                ErrorCode = 404,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = "RESOURCE NOT FOUND",
                ErrorDescription = "Account not found"
            }
        }, JsonOptions),
        Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    };

    private static PaApiResult BuildHotlisted() => new()
    {
        StatusCode = 422,
        Body = JsonSerializer.Serialize(new PaErrorResponse
        {
            Error = new PaErrorBody
            {
                ErrorCode = 422,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = "UNPROCESSABLE ENTITY",
                ErrorDescription = "Account is hotlisted and cannot be changed"
            }
        }, JsonOptions),
        Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    };

    private static PaApiResult BuildBadRequest(string errorMessage, string errorDescription) => new()
    {
        StatusCode = 400,
        Body = JsonSerializer.Serialize(new PaErrorResponse
        {
            Error = new PaErrorBody
            {
                ErrorCode = 400,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = errorMessage,
                ErrorDescription = errorDescription
            }
        }, JsonOptions),
        Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    };
}