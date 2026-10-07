using System.Text.Json;
using Npgsql;

namespace MuleaesaMainLambda;

public sealed class SaApiService
{
    private static readonly SaDatabaseClient DatabaseClient = new();

    /// <summary>GET /alive</summary>
    public Task<SaApiResult> GetAliveAsync()
    {
        return Task.FromResult(new SaApiResult
        {
            StatusCode = 200,
            Body = "UP"
        });
    }

    /// <summary>GET /ready</summary>
    public Task<SaApiResult> GetReadyAsync()
    {
        return Task.FromResult(new SaApiResult
        {
            StatusCode = 200,
            Body = "UP"
        });
    }

    /// <summary>GET /accounts</summary>
    public async Task<SaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        _ = headers;
        var email = HeaderHelper.GetHeader(query, "email");
        var accounts = await DatabaseClient.GetAccountsByEmailAsync(email).ConfigureAwait(false);
        return new SaApiResult
        {
            StatusCode = accounts.Count > 0 ? 200 : 204,
            Body = accounts.Count > 0 ? JsonSerializer.Serialize(accounts, SaJson.Default) : "[]"
        };
    }

    /// <summary>GET /accounts/{id}</summary>
    public async Task<SaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        _ = query;
        _ = headers;
        if (!Guid.TryParse(id, out var accountId))
        {
            return new SaApiResult
            {
                StatusCode = 204,
                Body = "[]"
            };
        }

        var account = await DatabaseClient.GetAccountByIdAsync(accountId).ConfigureAwait(false);
        if (account is null)
        {
            return new SaApiResult
            {
                StatusCode = 204,
                Body = "[]"
            };
        }

        return new SaApiResult
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(account, SaJson.Default)
        };
    }

    /// <summary>POST /accounts</summary>
    public async Task<SaApiResult> CreateAccountAsync(string? body, IDictionary<string, string>? headers)
    {
        _ = headers;
        var request = DeserializeRequest(body);
        var id = Guid.NewGuid();
        try
        {
            await DatabaseClient.InsertAccountAsync(id, request).ConfigureAwait(false);
            return new SaApiResult
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new SaIdResponse { Id = id.ToString() }, SaJson.Default)
            };
        }
        catch (PostgresException ex)
        {
            return BuildDatabaseError(ex);
        }
    }

    /// <summary>PUT /accounts/{id}</summary>
    public async Task<SaApiResult> UpdateAccountAsync(string id, string? body, IDictionary<string, string>? headers)
    {
        _ = headers;
        if (!Guid.TryParse(id, out var accountId))
        {
            return BuildNotFoundError();
        }

        var request = DeserializeRequest(body);
        try
        {
            var updated = await DatabaseClient.UpdateAccountAsync(accountId, request).ConfigureAwait(false);
            if (!updated)
            {
                return BuildAccountNotFoundError();
            }

            return new SaApiResult
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new SaIdResponse { Id = accountId.ToString() }, SaJson.Default)
            };
        }
        catch (PostgresException ex)
        {
            return BuildDatabaseError(ex);
        }
    }

    private static SaAccountRequest DeserializeRequest(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return new SaAccountRequest();
        }

        return JsonSerializer.Deserialize<SaAccountRequest>(body, SaJson.Default) ?? new SaAccountRequest();
    }

    private static SaApiResult BuildNotFoundError()
    {
        return BuildError(404, "RESOURCE NOT FOUND", "Resource not found");
    }

    private static SaApiResult BuildAccountNotFoundError()
    {
        return BuildError(400, "BAD REQUEST", "Account not found");
    }

    private static SaApiResult BuildDatabaseError(PostgresException ex)
    {
        return BuildError(400, $"{ex.SqlState} ERROR", ex.MessageText);
    }

    private static SaApiResult BuildError(int statusCode, string message, string description)
    {
        var payload = new SaErrorResponse
        {
            Error = new SaErrorBody
            {
                ErrorCode = statusCode,
                ErrorDateTime = DateTime.UtcNow,
                ErrorMessage = message,
                ErrorDescription = description
            }
        };

        return new SaApiResult
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(payload, SaJson.Default)
        };
    }
}