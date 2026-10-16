using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Azfcsharp8Function;

public sealed class AccountsService
{
    private readonly PostgreSqlClient client = new();

    public async Task<ApiResult> GetAccountsAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await client.GetByEmailAsync(email.Replace("'", "\\'"), cancellationToken);
            if (rows.Count == 0)
            {
                return new ApiResult { StatusCode = 204, Body = "[]" };
            }
            return new ApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(rows, AppHelpers.JsonOptions) };
        }
        catch (Exception ex)
        {
            AppLog.Error("Database failure", ex);
            return new ApiResult { StatusCode = 500, Body = JsonSerializer.Serialize(new ErrorResponse { error = new ErrorDetail { errorCode = 500, errorDateTime = DateTime.UtcNow, errorMessage = "MULE UNKNOWN ERROR", errorDescription = "An unexpected error occurred" } }, AppHelpers.JsonOptions) };
        }
    }

    public async Task<ApiResult> GetAccountByIdAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(id, out var guid))
            {
                return new ApiResult { StatusCode = 204, Body = "[]" };
            }
            var row = await client.GetByIdAsync(guid, cancellationToken);
            return row is null ? new ApiResult { StatusCode = 204, Body = "[]" } : new ApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(row, AppHelpers.JsonOptions) };
        }
        catch (Exception ex)
        {
            AppLog.Error("Database failure", ex);
            return new ApiResult { StatusCode = 500, Body = JsonSerializer.Serialize(new ErrorResponse { error = new ErrorDetail { errorCode = 500, errorDateTime = DateTime.UtcNow, errorMessage = "MULE UNKNOWN ERROR", errorDescription = "An unexpected error occurred" } }, AppHelpers.JsonOptions) };
        }
    }

    public async Task<ApiResult> CreateAccountAsync(string body, CancellationToken cancellationToken)
    {
        try
        {
            var request = JsonSerializer.Deserialize<AccountRequest>(body, AppHelpers.JsonOptions) ?? new AccountRequest();
            var id = await client.InsertAsync(request, cancellationToken);
            return new ApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(new IdResponse { id = id }, AppHelpers.JsonOptions) };
        }
        catch (Exception ex)
        {
            AppLog.Error("Database failure", ex);
            return ErrorFromDb(ex);
        }
    }

    public async Task<ApiResult> UpdateAccountAsync(string id, string body, CancellationToken cancellationToken)
    {
        try
        {
            var request = JsonSerializer.Deserialize<AccountRequest>(body, AppHelpers.JsonOptions) ?? new AccountRequest();
            if (!Guid.TryParse(id, out var guid))
            {
                return AccountNotFound();
            }
            var updated = await client.UpdateAsync(guid, request, cancellationToken);
            return updated is null ? AccountNotFound() : new ApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(new IdResponse { id = updated }, AppHelpers.JsonOptions) };
        }
        catch (Exception ex)
        {
            AppLog.Error("Database failure", ex);
            return ErrorFromDb(ex);
        }
    }

    public ApiResult ApiKitBadRequest() => new()
    {
        StatusCode = 400,
        Body = JsonSerializer.Serialize(new ErrorResponse { error = new ErrorDetail { errorCode = 400, errorDateTime = DateTime.UtcNow, errorMessage = "BAD REQUEST", errorDescription = "BAD REQUEST" } }, AppHelpers.JsonOptions)
    };

    public ApiResult ApiKitUnsupportedMediaType() => new()
    {
        StatusCode = 415,
        Body = JsonSerializer.Serialize(new ErrorResponse { error = new ErrorDetail { errorCode = 415, errorDateTime = DateTime.UtcNow, errorMessage = "UNSUPPORTED MEDIA TYPE", errorDescription = "UNSUPPORTED MEDIA TYPE" } }, AppHelpers.JsonOptions)
    };

    private static ApiResult AccountNotFound() => new()
    {
        StatusCode = 400,
        Body = JsonSerializer.Serialize(new ErrorResponse { error = new ErrorDetail { errorCode = 400, errorDateTime = DateTime.UtcNow, errorMessage = "Account not found", errorDescription = "Account not found" } }, AppHelpers.JsonOptions)
    };

    private static ApiResult ErrorFromDb(Exception ex)
    {
        var sqlState = ex is PostgresException pg ? pg.SqlState : "MULE UNKNOWN ERROR";
        var message = ex is PostgresException pg2 ? pg2.MessageText : "An unexpected error occurred";
        return new ApiResult
        {
            StatusCode = 400,
            Body = JsonSerializer.Serialize(new ErrorResponse { error = new ErrorDetail { errorCode = 400, errorDateTime = DateTime.UtcNow, errorMessage = $"{sqlState} ERROR", errorDescription = message } }, AppHelpers.JsonOptions)
        };
    }
}
