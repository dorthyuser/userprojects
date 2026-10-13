using System.Text.Json;
using System.Text.RegularExpressions;

namespace BusTravelAccountsSaLambda;

public sealed class AccountsService
{
    private readonly Settings _settings;
    private readonly PostgresClient _client;

    public AccountsService(Settings settings)
    {
        _settings = settings;
        _client = new PostgresClient(settings);
    }

    public async Task<ApiResult> HandleAsync(string method, string path, IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body, CancellationToken cancellationToken)
    {
        var normalizedPath = PathHelper.StripBasePath(path, _settings.BasePath);
        if (string.Equals(normalizedPath, "/alive", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } };
        }

        if (string.Equals(normalizedPath, "/ready", StringComparison.OrdinalIgnoreCase))
        {
            var healthy = await _client.CanConnectAsync(cancellationToken);
            return new ApiResult { StatusCode = healthy ? 200 : 500, Body = healthy ? "UP" : "DOWN", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } };
        }

        try
        {
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && string.Equals(normalizedPath, "/accounts", StringComparison.OrdinalIgnoreCase))
            {
                return await GetAccountsAsync(query, cancellationToken);
            }

            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && Regex.IsMatch(normalizedPath, @"^/accounts/[^/]+$", RegexOptions.IgnoreCase))
            {
                return await GetAccountByIdAsync(normalizedPath["/accounts/".Length..], cancellationToken);
            }

            if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) && string.Equals(normalizedPath, "/accounts", StringComparison.OrdinalIgnoreCase))
            {
                return await CreateAccountAsync(body, cancellationToken);
            }

            if (string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase) && Regex.IsMatch(normalizedPath, @"^/accounts/[^/]+$", RegexOptions.IgnoreCase))
            {
                return await UpdateAccountAsync(normalizedPath["/accounts/".Length..], body, cancellationToken);
            }

            return new ApiResult { StatusCode = 404, Body = JsonSerializer.Serialize(new ErrorResponse { Error = new ErrorDetails { ErrorCode = 404, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "RESOURCE NOT FOUND" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (Exception ex)
        {
            AppLog.Error("Unhandled error", ex);
            return new ApiResult { StatusCode = 500, Body = JsonSerializer.Serialize(new ErrorResponse { Error = new ErrorDetails { ErrorCode = 500, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "MULE UNKNOWN ERROR", ErrorDescription = "An unexpected error occurred" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }

    private async Task<ApiResult> GetAccountsAsync(IDictionary<string, string>? query, CancellationToken cancellationToken)
    {
        var email = query != null && query.TryGetValue("email", out var value) ? value : string.Empty;
        email = email.Replace("'", "\\'");
        var rows = await _client.GetByEmailAsync(email, cancellationToken);
        if (rows.Count == 0)
        {
            return new ApiResult { StatusCode = 204, Body = "[]", Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }

        var response = rows.Select(MapRow).ToArray();
        return new ApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(response, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
    }

    private async Task<ApiResult> GetAccountByIdAsync(string idText, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(idText, out var id))
        {
            return new ApiResult { StatusCode = 204, Body = "[]", Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }

        var row = await _client.GetByIdAsync(id, cancellationToken);
        if (row is null)
        {
            return new ApiResult { StatusCode = 204, Body = "[]", Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }

        return new ApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(MapRow(row), JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
    }

    private async Task<ApiResult> CreateAccountAsync(string? body, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<AccountCreateRequest>(body ?? string.Empty, JsonHelper.Options) ?? new AccountCreateRequest();
        var model = new AccountWriteModel
        {
            Salutation = Trim(request.Salutation),
            FirstName = Trim(request.FirstName),
            LastName = Trim(request.LastName),
            PersonEmail = Trim(request.PersonEmail),
            PersonBirthdate = request.PersonBirthdate,
            Phone = Trim(request.Phone),
            MobilePhone = Trim(request.MobilePhone),
            MailingStreet = Trim(request.MailingStreet),
            MailingPostalCode = Trim(request.MailingPostalCode),
            MailingCity = Trim(request.MailingCity),
            MailingCountry = Trim(request.MailingCountry),
            AccountStatus = Trim(request.AccountStatus),
            AccountSource = Trim(request.AccountSource),
            Hotlisted = request.Hotlisted
        };

        try
        {
            var id = await _client.InsertAsync(model, cancellationToken);
            return new ApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(new { id }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (Exception ex)
        {
            AppLog.Error("Create account failed", ex);
            return new ApiResult { StatusCode = 400, Body = JsonSerializer.Serialize(new ErrorResponse { Error = new ErrorDetails { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "ERROR", ErrorDescription = ex.Message } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }

    private async Task<ApiResult> UpdateAccountAsync(string idText, string? body, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(idText, out var id))
        {
            return new ApiResult { StatusCode = 400, Body = JsonSerializer.Serialize(new ErrorResponse { Error = new ErrorDetails { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "Account not found", ErrorDescription = "Account not found" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }

        var request = JsonSerializer.Deserialize<AccountUpdateRequest>(body ?? string.Empty, JsonHelper.Options) ?? new AccountUpdateRequest();
        var model = new AccountWriteModel
        {
            Salutation = Trim(request.Salutation),
            FirstName = Trim(request.FirstName),
            LastName = Trim(request.LastName),
            PersonEmail = Trim(request.PersonEmail),
            PersonBirthdate = request.PersonBirthdate,
            Phone = Trim(request.Phone),
            MobilePhone = Trim(request.MobilePhone),
            MailingStreet = Trim(request.MailingStreet),
            MailingPostalCode = Trim(request.MailingPostalCode),
            MailingCity = Trim(request.MailingCity),
            MailingCountry = Trim(request.MailingCountry),
            AccountStatus = Trim(request.AccountStatus),
            AccountSource = Trim(request.AccountSource)
        };

        try
        {
            var result = await _client.UpdateAsync(id, model, cancellationToken);
            if (!result.Updated)
            {
                return new ApiResult { StatusCode = 400, Body = JsonSerializer.Serialize(new ErrorResponse { Error = new ErrorDetails { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "Account not found", ErrorDescription = "Account not found" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            }

            return new ApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(new { id = result.Id }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (Exception ex)
        {
            AppLog.Error("Update account failed", ex);
            return new ApiResult { StatusCode = 400, Body = JsonSerializer.Serialize(new ErrorResponse { Error = new ErrorDetails { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "ERROR", ErrorDescription = ex.Message } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }

    private static AccountResponse MapRow(AccountRow row)
    {
        return new AccountResponse
        {
            Id = row.Id.ToString(),
            Salutation = Trim(row.Salutation),
            FirstName = Trim(row.FirstName),
            LastName = Trim(row.LastName) ?? string.Empty,
            PersonBirthdate = row.PersonBirthdate,
            Phone = Trim(row.Phone),
            MobilePhone = Trim(row.MobilePhone),
            PersonEmail = Trim(row.PersonEmail) ?? string.Empty,
            MailingStreet = Trim(row.MailingStreet),
            MailingPostalCode = Trim(row.MailingPostalCode),
            MailingCity = Trim(row.MailingCity),
            MailingCountry = Trim(row.MailingCountry),
            AccountStatus = Trim(row.AccountStatus),
            AccountSource = Trim(row.AccountSource) ?? string.Empty,
            Hotlisted = row.Hotlisted
        };
    }

    private static string? Trim(string? value) => value?.Trim();
}