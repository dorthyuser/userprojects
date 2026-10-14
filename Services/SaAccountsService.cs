using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;

namespace BusTravelAccountsLambda;

public sealed class SaAccountsService
{
    private readonly SaSettings settings = new();

    public async Task<SaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        try
        {
            var email = RequestHelper.GetQuery(query, "email")?.Trim() ?? string.Empty;
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(@email)";
            command.Parameters.AddWithValue("email", email);
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<object>();
            while (await reader.ReadAsync())
            {
                rows.Add(MapRow(reader));
            }

            if (rows.Count == 0)
            {
                return new SaApiResult { StatusCode = 204, Body = "[]", Headers = ResponseHelper.JsonHeaders() };
            }

            return new SaApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(rows, JsonHelper.Options), Headers = ResponseHelper.JsonHeaders() };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return ErrorFromPostgres(ex, 400);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return UnexpectedError();
        }
    }

    public async Task<SaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        try
        {
            if (!Guid.TryParse(id?.Trim(), out var guid))
            {
                return new SaApiResult { StatusCode = 204, Body = "[]", Headers = ResponseHelper.JsonHeaders() };
            }

            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = @id";
            command.Parameters.AddWithValue("id", guid);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new SaApiResult { StatusCode = 204, Body = "[]", Headers = ResponseHelper.JsonHeaders() };
            }

            var body = JsonSerializer.Serialize(MapRow(reader), JsonHelper.Options);
            return new SaApiResult { StatusCode = 200, Body = body, Headers = ResponseHelper.JsonHeaders() };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return ErrorFromPostgres(ex, 400);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return UnexpectedError();
        }
    }

    public async Task<SaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        try
        {
            using var document = JsonDocument.Parse(body ?? "{}");
            var root = document.RootElement;
            var id = Guid.NewGuid();
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = @"insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, created_at, updated_at) values (@id, @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source, coalesce(@hotlisted, false), now(), now())";
            AddInsertParameters(command, root, id);
            await command.ExecuteNonQueryAsync();
            return new SaApiResult { StatusCode = 201, Body = JsonSerializer.Serialize(new { id }, JsonHelper.Options), Headers = ResponseHelper.JsonHeaders() };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return ErrorFromPostgres(ex, 400);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return UnexpectedError();
        }
    }

    public async Task<SaApiResult> PutAccountByIdAsync(string id, string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        try
        {
            if (!Guid.TryParse(id?.Trim(), out var guid))
            {
                return NotFound();
            }

            using var document = JsonDocument.Parse(body ?? "{}");
            var root = document.RootElement;
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using (var lookup = connection.CreateCommand())
            {
                lookup.CommandText = @"select hotlisted from public.sa_accounts where id = @id";
                lookup.Parameters.AddWithValue("id", guid);
                var lookupResult = await lookup.ExecuteScalarAsync();
                if (lookupResult is null || lookupResult is DBNull)
                {
                    return NotFound();
                }

                if (lookupResult is bool hotlisted && hotlisted)
                {
                    return new SaApiResult { StatusCode = 422, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorDetails { ErrorCode = 422, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "UNPROCESSABLE ENTITY", ErrorDescription = "Account is hotlisted and cannot be changed" } }, JsonHelper.Options), Headers = ResponseHelper.JsonHeaders() };
                }
            }

            await using var command = connection.CreateCommand();
            command.CommandText = @"update public.sa_accounts set salutation = @salutation, first_name = @first_name, last_name = @last_name, person_email = @person_email, person_birthdate = @person_birthdate, phone = @phone, mobile_phone = @mobile_phone, mailing_street = @mailing_street, mailing_postal_code = @mailing_postal_code, mailing_city = @mailing_city, mailing_country = @mailing_country, account_status = @account_status, account_source = @account_source, updated_at = now() where id = @id";
            AddInsertParameters(command, root, guid);
            var affected = await command.ExecuteNonQueryAsync();
            if (affected == 0)
            {
                return NotFound();
            }

            return new SaApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(new { id = guid }, JsonHelper.Options), Headers = ResponseHelper.JsonHeaders() };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return ErrorFromPostgres(ex, 400);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return UnexpectedError();
        }
    }

    public Task<SaApiResult> AliveAsync() => Task.FromResult(new SaApiResult { StatusCode = 200, Body = "UP", Headers = ResponseHelper.TextHeaders() });

    public Task<SaApiResult> ReadyAsync() => Task.FromResult(new SaApiResult { StatusCode = 200, Body = "UP", Headers = ResponseHelper.TextHeaders() });

    private NpgsqlConnection CreateConnection()
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = settings.Database.Host,
            Port = int.TryParse(settings.Database.Port, out var port) ? port : 5432,
            Database = settings.Database.Database,
            Username = settings.Database.Username,
            Password = settings.Database.Password,
            SslMode = SslMode.Require,
            TrustServerCertificate = false
        };
        return new NpgsqlConnection(csb.ConnectionString);
    }

    private static object MapRow(NpgsqlDataReader reader)
    {
        return new
        {
            id = reader.GetGuid(reader.GetOrdinal("id")).ToString(),
            salutation = reader.IsDBNull(reader.GetOrdinal("salutation")) ? null : reader.GetString(reader.GetOrdinal("salutation")).Trim(),
            firstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString(reader.GetOrdinal("first_name")).Trim(),
            lastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString(reader.GetOrdinal("last_name")).Trim(),
            personEmail = reader.IsDBNull(reader.GetOrdinal("person_email")) ? null : reader.GetString(reader.GetOrdinal("person_email")).Trim(),
            personBirthdate = reader.IsDBNull(reader.GetOrdinal("person_birthdate")) ? (DateOnly?)null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("person_birthdate"))),
            phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")).Trim(),
            mobilePhone = reader.IsDBNull(reader.GetOrdinal("mobile_phone")) ? null : reader.GetString(reader.GetOrdinal("mobile_phone")).Trim(),
            mailingStreet = reader.IsDBNull(reader.GetOrdinal("mailing_street")) ? null : reader.GetString(reader.GetOrdinal("mailing_street")).Trim(),
            mailingPostalCode = reader.IsDBNull(reader.GetOrdinal("mailing_postal_code")) ? null : reader.GetString(reader.GetOrdinal("mailing_postal_code")).Trim(),
            mailingCity = reader.IsDBNull(reader.GetOrdinal("mailing_city")) ? null : reader.GetString(reader.GetOrdinal("mailing_city")).Trim(),
            mailingCountry = reader.IsDBNull(reader.GetOrdinal("mailing_country")) ? null : reader.GetString(reader.GetOrdinal("mailing_country")).Trim(),
            accountStatus = reader.IsDBNull(reader.GetOrdinal("account_status")) ? null : reader.GetString(reader.GetOrdinal("account_status")).Trim(),
            accountSource = reader.IsDBNull(reader.GetOrdinal("account_source")) ? string.Empty : reader.GetString(reader.GetOrdinal("account_source")).Trim(),
            hotlisted = !reader.IsDBNull(reader.GetOrdinal("hotlisted")) && reader.GetBoolean(reader.GetOrdinal("hotlisted"))
        };
    }

    private static void AddInsertParameters(NpgsqlCommand command, JsonElement root, Guid id)
    {
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("salutation", GetString(root, "salutation"));
        command.Parameters.AddWithValue("first_name", GetString(root, "firstName"));
        command.Parameters.AddWithValue("last_name", GetString(root, "lastName"));
        command.Parameters.AddWithValue("person_email", GetString(root, "personEmail"));
        command.Parameters.AddWithValue("person_birthdate", GetDateOnly(root, "personBirthdate"));
        command.Parameters.AddWithValue("phone", GetString(root, "phone"));
        command.Parameters.AddWithValue("mobile_phone", GetString(root, "mobilePhone"));
        command.Parameters.AddWithValue("mailing_street", GetString(root, "mailingStreet"));
        command.Parameters.AddWithValue("mailing_postal_code", GetString(root, "mailingPostalCode"));
        command.Parameters.AddWithValue("mailing_city", GetString(root, "mailingCity"));
        command.Parameters.AddWithValue("mailing_country", GetString(root, "mailingCountry"));
        command.Parameters.AddWithValue("account_status", GetString(root, "accountStatus"));
        command.Parameters.AddWithValue("account_source", GetString(root, "accountSource"));
        command.Parameters.AddWithValue("hotlisted", GetBool(root, "hotlisted"));
    }

    private static string? GetString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : value.GetRawText();
    }

    private static DateOnly? GetDateOnly(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String && DateOnly.TryParse(value.GetString(), out var date))
        {
            return date;
        }

        return null;
    }

    private static bool? GetBool(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.True || (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var b) && b);
    }

    private static SaApiResult NotFound() => new() { StatusCode = 400, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorDetails { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "Account not found" } }, JsonHelper.Options), Headers = ResponseHelper.JsonHeaders() };

    private static SaApiResult ErrorFromPostgres(PostgresException ex, int status) => new()
    {
        StatusCode = status,
        Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorDetails { ErrorCode = status, ErrorDateTime = DateTime.UtcNow, ErrorMessage = $"{ex.SqlState} ERROR", ErrorDescription = ex.MessageText } }, JsonHelper.Options),
        Headers = ResponseHelper.JsonHeaders()
    };

    private static SaApiResult UnexpectedError() => new()
    {
        StatusCode = 500,
        Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorDetails { ErrorCode = 500, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "MULE UNKNOWN ERROR", ErrorDescription = "An unexpected error occurred" } }, JsonHelper.Options),
        Headers = ResponseHelper.JsonHeaders()
    };
}
