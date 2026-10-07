using System.Data;
using System.Globalization;
using System.Text.Json;
using Npgsql;

namespace MulecombineMainLambda;

public sealed class SaApiService
{
    private static readonly JsonSerializerOptions JsonOptions = SaJsonOptions.Create();
    private static readonly string ConnectionString = BuildConnectionString();

    public async Task<SaApiResult> GetAliveAsync()
    {
        return await Task.FromResult(new SaApiResult
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new SaHealthResponse { Status = "UP" }, JsonOptions)
        });
    }

    public async Task<SaApiResult> GetReadyAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = new NpgsqlCommand("select 1", connection);
        await command.ExecuteScalarAsync(CancellationToken.None);
        return new SaApiResult
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new SaHealthResponse { Status = "UP" }, JsonOptions)
        };
    }

    public async Task<SaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var email = SaHeaderHelper.GetHeader(query, "email");
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken.None);

        await using var command = new NpgsqlCommand(@"select id, salutation, first_name, last_name, person_birthdate, phone, mobile_phone, person_email, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(@email)", connection);
        command.Parameters.AddWithValue("email", email ?? string.Empty);

        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        var results = new List<SaAccountResponse>();
        while (await reader.ReadAsync(CancellationToken.None))
        {
            results.Add(ReadAccount(reader));
        }

        if (results.Count == 0)
        {
            return new SaApiResult { StatusCode = 204, Body = "[]" };
        }

        return new SaApiResult
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(results, JsonOptions)
        };
    }

    public async Task<SaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        if (!Guid.TryParse(id, out var accountId))
        {
            return new SaApiResult { StatusCode = 204, Body = "[]" };
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = new NpgsqlCommand(@"select id, salutation, first_name, last_name, person_birthdate, phone, mobile_phone, person_email, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = @id", connection);
        command.Parameters.AddWithValue("id", accountId);

        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        if (!await reader.ReadAsync(CancellationToken.None))
        {
            return new SaApiResult { StatusCode = 204, Body = "[]" };
        }

        var account = ReadAccount(reader);
        return new SaApiResult
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(account, JsonOptions)
        };
    }

    public async Task<SaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? headers)
    {
        var request = DeserializeRequest(body);
        var id = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = new NpgsqlCommand(@"insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, created_at, updated_at) values (@id, @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source, @hotlisted, now(), now())", connection);
        AddParameters(command, id, request, includeUpdatedAt: false);
        await command.ExecuteNonQueryAsync(CancellationToken.None);
        return new SaApiResult
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new SaCreateAccountResponse { Id = id.ToString() }, JsonOptions)
        };
    }

    public async Task<SaApiResult> PutAccountAsync(string id, string? body, IDictionary<string, string>? headers)
    {
        if (!Guid.TryParse(id, out var accountId))
        {
            return BuildBadRequest("400", "Account not found");
        }

        var request = DeserializeRequest(body);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = new NpgsqlCommand(@"update public.sa_accounts set salutation = @salutation, first_name = @first_name, last_name = @last_name, person_email = @person_email, person_birthdate = @person_birthdate, phone = @phone, mobile_phone = @mobile_phone, mailing_street = @mailing_street, mailing_postal_code = @mailing_postal_code, mailing_city = @mailing_city, mailing_country = @mailing_country, account_status = @account_status, account_source = @account_source, hotlisted = @hotlisted, updated_at = now() where id = @id", connection);
        AddParameters(command, accountId, request, includeUpdatedAt: true);
        var rows = await command.ExecuteNonQueryAsync(CancellationToken.None);
        if (rows == 0)
        {
            return BuildBadRequest("400", "Account not found");
        }

        return new SaApiResult
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new SaCreateAccountResponse { Id = accountId.ToString() }, JsonOptions)
        };
    }

    public SaApiResult BuildBadRequest(string errorMessage, string errorDescription)
    {
        return new SaApiResult
        {
            StatusCode = 400,
            Body = JsonSerializer.Serialize(new SaErrorResponse
            {
                Error = new SaErrorBody
                {
                    ErrorCode = 400,
                    ErrorDateTime = DateTimeOffset.UtcNow,
                    ErrorMessage = errorMessage,
                    ErrorDescription = errorDescription
                }
            }, JsonOptions)
        };
    }

    public SaApiResult BuildNotFound(string errorDescription)
    {
        return new SaApiResult
        {
            StatusCode = 404,
            Body = JsonSerializer.Serialize(new SaErrorResponse
            {
                Error = new SaErrorBody
                {
                    ErrorCode = 404,
                    ErrorDateTime = DateTimeOffset.UtcNow,
                    ErrorMessage = "RESOURCE NOT FOUND",
                    ErrorDescription = errorDescription
                }
            }, JsonOptions)
        };
    }

    public SaApiResult BuildMethodNotAllowed(string errorDescription)
    {
        return new SaApiResult
        {
            StatusCode = 405,
            Body = JsonSerializer.Serialize(new SaErrorResponse
            {
                Error = new SaErrorBody
                {
                    ErrorCode = 405,
                    ErrorDateTime = DateTimeOffset.UtcNow,
                    ErrorMessage = "METHOD NOT ALLOWED",
                    ErrorDescription = errorDescription
                }
            }, JsonOptions)
        };
    }

    public SaApiResult BuildUnsupportedMediaType(string errorDescription)
    {
        return new SaApiResult
        {
            StatusCode = 415,
            Body = JsonSerializer.Serialize(new SaErrorResponse
            {
                Error = new SaErrorBody
                {
                    ErrorCode = 415,
                    ErrorDateTime = DateTimeOffset.UtcNow,
                    ErrorMessage = "UNSUPPORTED MEDIA TYPE",
                    ErrorDescription = errorDescription
                }
            }, JsonOptions)
        };
    }

    public SaApiResult BuildUnexpectedError(Exception exception)
    {
        return new SaApiResult
        {
            StatusCode = 500,
            Body = JsonSerializer.Serialize(new SaErrorResponse
            {
                Error = new SaErrorBody
                {
                    ErrorCode = 500,
                    ErrorDateTime = DateTimeOffset.UtcNow,
                    ErrorMessage = "SYSTEM ERROR",
                    ErrorDescription = "An unexpected error occurred."
                }
            }, JsonOptions)
        };
    }

    private static string BuildConnectionString()
    {
        var (host, port, database, username, password) = SaSettings.GetDatabaseConnection();
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = database,
            Username = username,
            Password = password,
            Pooling = true,
            IncludeErrorDetail = false
        };
        return builder.ConnectionString;
    }

    private static SaAccountRequest DeserializeRequest(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return new SaAccountRequest();
        }

        return JsonSerializer.Deserialize<SaAccountRequest>(body, JsonOptions) ?? new SaAccountRequest();
    }

    private static void AddParameters(NpgsqlCommand command, Guid id, SaAccountRequest request, bool includeUpdatedAt)
    {
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("salutation", (object?)Trim(request.Salutation) ?? DBNull.Value);
        command.Parameters.AddWithValue("first_name", (object?)Trim(request.FirstName) ?? DBNull.Value);
        command.Parameters.AddWithValue("last_name", (object?)Trim(request.LastName) ?? DBNull.Value);
        command.Parameters.AddWithValue("person_email", (object?)Trim(request.PersonEmail) ?? DBNull.Value);
        command.Parameters.AddWithValue("person_birthdate", request.PersonBirthdate.HasValue ? request.PersonBirthdate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("phone", (object?)Trim(request.Phone) ?? DBNull.Value);
        command.Parameters.AddWithValue("mobile_phone", (object?)Trim(request.MobilePhone) ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_street", (object?)Trim(request.MailingStreet) ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_postal_code", (object?)Trim(request.MailingPostalCode) ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_city", (object?)Trim(request.MailingCity) ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_country", (object?)Trim(request.MailingCountry) ?? DBNull.Value);
        command.Parameters.AddWithValue("account_status", (object?)Trim(request.AccountStatus) ?? DBNull.Value);
        command.Parameters.AddWithValue("account_source", (object?)Trim(request.AccountSource) ?? DBNull.Value);
        command.Parameters.AddWithValue("hotlisted", request.Hotlisted.HasValue ? request.Hotlisted.Value : DBNull.Value);
    }

    private static SaAccountResponse ReadAccount(NpgsqlDataReader reader)
    {
        return new SaAccountResponse
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")).ToString(),
            Salutation = ReadString(reader, "salutation"),
            FirstName = ReadString(reader, "first_name"),
            LastName = ReadString(reader, "last_name"),
            PersonBirthdate = reader.IsDBNull(reader.GetOrdinal("person_birthdate")) ? null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("person_birthdate"))),
            Phone = ReadString(reader, "phone"),
            MobilePhone = ReadString(reader, "mobile_phone"),
            PersonEmail = ReadString(reader, "person_email"),
            MailingStreet = ReadString(reader, "mailing_street"),
            MailingPostalCode = ReadString(reader, "mailing_postal_code"),
            MailingCity = ReadString(reader, "mailing_city"),
            MailingCountry = ReadString(reader, "mailing_country"),
            AccountStatus = ReadString(reader, "account_status"),
            AccountSource = ReadString(reader, "account_source"),
            Hotlisted = reader.IsDBNull(reader.GetOrdinal("hotlisted")) ? null : reader.GetBoolean(reader.GetOrdinal("hotlisted"))
        };
    }

    private static string? ReadString(NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}