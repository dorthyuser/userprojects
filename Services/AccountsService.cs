using Npgsql;

namespace BusTravelAccountsSaLambda;

public sealed class AccountsService
{
    private readonly PostgresClient client;

    public AccountsService(Settings settings)
    {
        client = new PostgresClient(settings.Database);
    }

    public async Task<APIGatewayProxyResponse> GetAccountsAsync(IDictionary<string, string>? query)
    {
        var email = HeaderHelper.GetValue(query, "email");
        var rows = await client.GetAccountsByEmailAsync(email ?? string.Empty, CancellationToken.None);
        if (rows.Count == 0)
        {
            return ResponseFactory.NoContentArray();
        }

        var response = rows.Select(Map).ToList();
        return ResponseFactory.Json(200, response);
    }

    public async Task<APIGatewayProxyResponse> GetAccountByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return ResponseFactory.NoContentArray();
        }

        var row = await client.GetAccountByIdAsync(guid, CancellationToken.None);
        if (row is null)
        {
            return ResponseFactory.NoContentArray();
        }

        return ResponseFactory.Json(200, Map(row));
    }

    public async Task<APIGatewayProxyResponse> CreateAccountAsync(IDictionary<string, string>? headers, string? body, bool isBase64Encoded)
    {
        var request = RequestBodyHelper.Parse<AccountCreateRequest>(body, isBase64Encoded);
        try
        {
            var id = await client.InsertAccountAsync(request, CancellationToken.None);
            return ResponseFactory.Json(200, new IdResponse { Id = id.ToString() });
        }
        catch (PostgresException ex)
        {
            AppLog.Error(ex.Message, ex);
            return ResponseFactory.DatabaseError(ex.SqlState, ex.Message);
        }
    }

    public async Task<APIGatewayProxyResponse> UpdateAccountAsync(string id, IDictionary<string, string>? headers, string? body, bool isBase64Encoded)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return ResponseFactory.AccountNotFound();
        }

        var request = RequestBodyHelper.Parse<AccountUpdateRequest>(body, isBase64Encoded);
        try
        {
            var updated = await client.UpdateAccountAsync(guid, request, CancellationToken.None);
            if (!updated)
            {
                return ResponseFactory.AccountNotFound();
            }

            return ResponseFactory.Json(200, new IdResponse { Id = guid.ToString() });
        }
        catch (PostgresException ex)
        {
            AppLog.Error(ex.Message, ex);
            return ResponseFactory.DatabaseError(ex.SqlState, ex.Message);
        }
    }

    private static AccountResponse Map(DbAccountRow row)
    {
        return new AccountResponse
        {
            Id = row.Id.ToString(),
            Salutation = Trim(row.Salutation),
            FirstName = Trim(row.FirstName),
            LastName = Trim(row.LastName),
            PersonBirthdate = row.PersonBirthdate,
            Phone = Trim(row.Phone),
            MobilePhone = Trim(row.MobilePhone),
            PersonEmail = Trim(row.PersonEmail),
            MailingStreet = Trim(row.MailingStreet),
            MailingPostalCode = Trim(row.MailingPostalCode),
            MailingCity = Trim(row.MailingCity),
            MailingCountry = Trim(row.MailingCountry),
            AccountStatus = Trim(row.AccountStatus),
            AccountSource = Trim(row.AccountSource) ?? string.Empty,
            Hotlisted = row.Hotlisted
        };
    }

    private static string? Trim(string? value) => value is null ? null : value.Trim();
}
