using System.Text.Json;

namespace BusTravelAccountsSaLambda;

public sealed class AccountsService
{
    private readonly PostgresClient client;

    public AccountsService(Settings settings)
    {
        client = new PostgresClient(settings);
    }

    public async Task<APIGatewayProxyResponse> GetAccountsAsync(IDictionary<string, string>? query)
    {
        var email = HeaderHelper.GetQuery(query, "email");
        if (string.IsNullOrWhiteSpace(email))
        {
            return ErrorResponses.BadRequest();
        }

        try
        {
            var rows = await client.GetAccountsByEmailAsync(email);
            if (rows.Count == 0)
            {
                return ResponseHelper.Text(204, "[]");
            }

            return ResponseHelper.Json(200, rows.Select(AccountResponseDto.From).ToArray());
        }
        catch (Exception ex)
        {
            AppLog.Error("Database error while listing accounts", ex);
            return ErrorResponses.InternalServerError();
        }
    }

    public async Task<APIGatewayProxyResponse> GetAccountByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return ErrorResponses.BadRequest();
        }

        try
        {
            var row = await client.GetAccountByIdAsync(guid);
            return row is null ? ResponseHelper.Text(204, "[]") : ResponseHelper.Json(200, AccountResponseDto.From(row));
        }
        catch (Exception ex)
        {
            AppLog.Error("Database error while getting account by id", ex);
            return ErrorResponses.InternalServerError();
        }
    }

    public async Task<APIGatewayProxyResponse> PostAccountAsync(string? body, bool isBase64Encoded)
    {
        try
        {
            var dto = AccountWriteDto.FromRequestBody(BodyHelper.DecodeBody(body, isBase64Encoded));
            var id = await client.InsertAccountAsync(dto);
            return ResponseHelper.Json(200, new { id });
        }
        catch (JsonException ex)
        {
            AppLog.Error("Invalid JSON request body", ex);
            return ErrorResponses.BadRequest();
        }
        catch (Exception ex)
        {
            AppLog.Error("Database error while creating account", ex);
            return ErrorResponses.InternalServerError();
        }
    }

    public async Task<APIGatewayProxyResponse> PutAccountAsync(string id, string? body, bool isBase64Encoded)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return ErrorResponses.BadRequest();
        }

        try
        {
            var dto = AccountWriteDto.FromRequestBody(BodyHelper.DecodeBody(body, isBase64Encoded));
            var updated = await client.UpdateAccountAsync(guid, dto);
            if (!updated)
            {
                return ErrorResponses.InternalServerError();
            }

            return ResponseHelper.Json(200, new { id = guid });
        }
        catch (JsonException ex)
        {
            AppLog.Error("Invalid JSON request body", ex);
            return ErrorResponses.BadRequest();
        }
        catch (Exception ex)
        {
            AppLog.Error("Database error while updating account", ex);
            return ErrorResponses.InternalServerError();
        }
    }

    public async Task<APIGatewayProxyResponse> ReadyAsync()
    {
        var ready = await client.CanConnectAsync();
        return ready ? ResponseHelper.Text(200, "UP") : ResponseHelper.Text(500, "DOWN");
    }
}
