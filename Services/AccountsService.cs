using System.Text.Json;
namespace BusTravelAccountsSaMainLambda;

public sealed class AccountsService
{
    private readonly AccountsDbClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public AccountsService(AccountsDbClient client, JsonSerializerOptions jsonOptions)
    {
        _client = client;
        _jsonOptions = jsonOptions;
    }

    public async Task<ServiceResult> GetAccountsAsync(IDictionary<string, string>? queryStringParameters, string correlationId, CancellationToken cancellationToken)
    {
        var email = HeaderHelper.GetQueryValue(queryStringParameters, "email");
        var accounts = await _client.GetAccountsAsync(email, cancellationToken);
        if (accounts.Count == 0)
        {
            return new ServiceResult(204, "[]");
        }

        return new ServiceResult(200, JsonSerializer.Serialize(accounts, _jsonOptions));
    }

    public async Task<ServiceResult> GetAccountByIdAsync(string id, string correlationId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var parsedId))
        {
            return new ServiceResult(204, "[]");
        }

        var account = await _client.GetAccountByIdAsync(parsedId, cancellationToken);
        if (account is null)
        {
            return new ServiceResult(204, "[]");
        }

        return new ServiceResult(200, JsonSerializer.Serialize(account, _jsonOptions));
    }

    public async Task<ServiceResult> CreateAccountAsync(string body, string correlationId, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<CreateAccountRequest>(body, _jsonOptions) ?? throw new ApiKitBadRequestException("BAD REQUEST");
        var created = await _client.CreateAccountAsync(request, cancellationToken);
        return new ServiceResult(200, JsonSerializer.Serialize(new CreateAccountResponse(created.Id), _jsonOptions));
    }

    public async Task<ServiceResult> UpdateAccountAsync(string id, string body, string correlationId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var parsedId))
        {
            return new ServiceResult(400, JsonSerializer.Serialize(ErrorResponseFactory.BadRequest("Account not found"), _jsonOptions));
        }

        var request = JsonSerializer.Deserialize<UpdateAccountRequest>(body, _jsonOptions) ?? throw new ApiKitBadRequestException("BAD REQUEST");
        var updated = await _client.UpdateAccountAsync(parsedId, request, cancellationToken);
        if (!updated)
        {
            return new ServiceResult(400, JsonSerializer.Serialize(ErrorResponseFactory.BadRequest("Account not found"), _jsonOptions));
        }

        return new ServiceResult(200, JsonSerializer.Serialize(new CreateAccountResponse(id), _jsonOptions));
    }
}

public sealed record ServiceResult(int StatusCode, string Body);
