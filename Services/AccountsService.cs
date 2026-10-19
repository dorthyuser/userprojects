using System.Text.Json;

namespace BusTravelAccountsSaLambda;

public sealed class AccountsService
{
    private readonly PostgresClient _client = new();

    public async Task<ApiResult> GetAccountsAsync(IDictionary<string, string>? query, CancellationToken cancellationToken)
    {
        try
        {
            var email = GetValue(query, "email");
            var accounts = await _client.GetAccountsByEmailAsync(email ?? string.Empty, cancellationToken).ConfigureAwait(false);
            if (accounts.Count == 0)
            {
                return new ApiResult { StatusCode = 204, Body = "[]", Headers = JsonHeaders() };
            }

            return new ApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(accounts, JsonOptions.Default), Headers = JsonHeaders() };
        }
        catch (ApiException ex)
        {
            AppLog.Error("Handled API error", ex);
            return BuildError(ex.StatusCode, ex.ErrorMessage, ex.ErrorDescription);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return BuildError(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
        }
    }

    public async Task<ApiResult> GetAccountByIdAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(id, out var guid))
            {
                throw new ApiException(400, "BAD REQUEST", "Invalid id");
            }

            var account = await _client.GetAccountByIdAsync(guid, cancellationToken).ConfigureAwait(false);
            if (account is null)
            {
                return new ApiResult { StatusCode = 204, Body = "[]", Headers = JsonHeaders() };
            }

            return new ApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(account, JsonOptions.Default), Headers = JsonHeaders() };
        }
        catch (ApiException ex)
        {
            AppLog.Error("Handled API error", ex);
            return BuildError(ex.StatusCode, ex.ErrorMessage, ex.ErrorDescription);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return BuildError(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
        }
    }

    public async Task<ApiResult> CreateAccountAsync(string? body, CancellationToken cancellationToken)
    {
        try
        {
            var request = Deserialize(body);
            var id = await _client.InsertAccountAsync(request, cancellationToken).ConfigureAwait(false);
            return new ApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(new IdResponse { Id = id }, JsonOptions.Default), Headers = JsonHeaders() };
        }
        catch (ApiException ex)
        {
            AppLog.Error("Handled API error", ex);
            return BuildError(ex.StatusCode, ex.ErrorMessage, ex.ErrorDescription);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return BuildError(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
        }
    }

    public async Task<ApiResult> UpdateAccountAsync(string id, string? body, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(id, out var guid))
            {
                throw new ApiException(400, "BAD REQUEST", "Invalid id");
            }

            var request = Deserialize(body);
            var updatedId = await _client.UpdateAccountAsync(guid, request, cancellationToken).ConfigureAwait(false);
            return new ApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(new IdResponse { Id = updatedId }, JsonOptions.Default), Headers = JsonHeaders() };
        }
        catch (ApiException ex)
        {
            AppLog.Error("Handled API error", ex);
            return BuildError(ex.StatusCode, ex.ErrorMessage, ex.ErrorDescription);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return BuildError(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
        }
    }

    public ApiResult Alive() => new() { StatusCode = 200, Body = "UP", Headers = TextHeaders() };
    public ApiResult Ready() => new() { StatusCode = 200, Body = "UP", Headers = TextHeaders() };

    private static AccountRequest Deserialize(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return new AccountRequest();
        }

        return JsonSerializer.Deserialize<AccountRequest>(body, JsonOptions.Default) ?? new AccountRequest();
    }

    private static string? GetValue(IDictionary<string, string>? query, string key)
    {
        if (query is null)
        {
            return null;
        }

        return query.TryGetValue(key, out var value) ? value : null;
    }

    private static ApiResult BuildError(int statusCode, string message, string description)
    {
        var error = new ErrorResponse
        {
            Error = new ErrorDetails
            {
                ErrorCode = statusCode,
                ErrorDateTime = DateTime.UtcNow,
                ErrorMessage = message,
                ErrorDescription = description
            }
        };

        return new ApiResult { StatusCode = statusCode, Body = JsonSerializer.Serialize(error, JsonOptions.Default), Headers = JsonHeaders() };
    }

    private static IDictionary<string, string> JsonHeaders() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" };
    private static IDictionary<string, string> TextHeaders() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "text/plain" };
}