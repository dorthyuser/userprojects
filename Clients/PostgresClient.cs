using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace BusTravelAccountsSaLambda;

public sealed class PostgresClient
{
    private static readonly HttpClient NotUsed = new();
    private readonly string connectionString;

    public PostgresClient(DatabaseSettings settings)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = settings.Host,
            Port = settings.Port,
            Database = settings.Database,
            Username = settings.Username,
            Password = settings.Password,
            SslMode = SslMode.Require,
            TrustServerCertificate = false
        };
        connectionString = builder.ConnectionString;
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            var ok = connection.State == ConnectionState.Open;
            AppLog.Info($"PostgreSQL CONNECT readiness check {(ok ? "200" : "500")}");
            return ok;
        }
        catch (Exception ex)
        {
            AppLog.Error("PostgreSQL CONNECT readiness check failed", ex);
            throw;
        }
    }

    public async Task<List<DbAccountRow>> GetAccountsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        const string sql = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(@email) order by id";
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("email", email);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<DbAccountRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(ReadRow(reader));
            }
            AppLog.Info($"PostgreSQL SELECT public.sa_accounts GET /accounts rows={rows.Count}");
            return rows;
        }
        catch (Exception ex)
        {
            AppLog.Error("PostgreSQL SELECT public.sa_accounts GET /accounts failed", ex);
            throw;
        }
    }

    public async Task<DbAccountRow?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = @id";
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var row = await reader.ReadAsync(cancellationToken) ? ReadRow(reader) : null;
            AppLog.Info($"PostgreSQL SELECT public.sa_accounts GET /accounts/{{id}} {(row is null ? "0" : "1")}");
            return row;
        }
        catch (Exception ex)
        {
            AppLog.Error("PostgreSQL SELECT public.sa_accounts GET /accounts/{id} failed", ex);
            throw;
        }
    }

    public async Task<Guid> InsertAccountAsync(AccountCreateRequest request, CancellationToken cancellationToken)
    {
        const string sql = @"insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted) values (@id, @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source, coalesce(@hotlisted, false)) returning id";
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            var id = Guid.NewGuid();
            command.Parameters.AddWithValue("id", id);
            AddNullable(command, "salutation", Trim(request.Salutation));
            AddNullable(command, "first_name", Trim(request.FirstName));
            AddNullable(command, "last_name", Trim(request.LastName));
            AddNullable(command, "person_email", Trim(request.PersonEmail));
            AddNullable(command, "person_birthdate", request.PersonBirthdate);
            AddNullable(command, "phone", Trim(request.Phone));
            AddNullable(command, "mobile_phone", Trim(request.MobilePhone));
            AddNullable(command, "mailing_street", Trim(request.MailingStreet));
            AddNullable(command, "mailing_postal_code", Trim(request.MailingPostalCode));
            AddNullable(command, "mailing_city", Trim(request.MailingCity));
            AddNullable(command, "mailing_country", Trim(request.MailingCountry));
            AddNullable(command, "account_status", Trim(request.AccountStatus));
            AddNullable(command, "account_source", Trim(request.AccountSource));
            AddNullable(command, "hotlisted", request.Hotlisted);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var createdId = result is Guid guid ? guid : id;
            AppLog.Info("PostgreSQL INSERT public.sa_accounts POST /accounts 1");
            return createdId;
        }
        catch (Exception ex)
        {
            AppLog.Error("PostgreSQL INSERT public.sa_accounts POST /accounts failed", ex);
            throw;
        }
    }

    public async Task<bool> UpdateAccountAsync(Guid id, AccountUpdateRequest request, CancellationToken cancellationToken)
    {
        var sets = new List<string>();
        var commandParameters = new List<NpgsqlParameter>();
        AddSet(sets, commandParameters, "salutation", Trim(request.Salutation));
        AddSet(sets, commandParameters, "first_name", Trim(request.FirstName));
        AddSet(sets, commandParameters, "last_name", Trim(request.LastName));
        AddSet(sets, commandParameters, "person_email", Trim(request.PersonEmail));
        AddSet(sets, commandParameters, "person_birthdate", request.PersonBirthdate);
        AddSet(sets, commandParameters, "phone", Trim(request.Phone));
        AddSet(sets, commandParameters, "mobile_phone", Trim(request.MobilePhone));
        AddSet(sets, commandParameters, "mailing_street", Trim(request.MailingStreet));
        AddSet(sets, commandParameters, "mailing_postal_code", Trim(request.MailingPostalCode));
        AddSet(sets, commandParameters, "mailing_city", Trim(request.MailingCity));
        AddSet(sets, commandParameters, "mailing_country", Trim(request.MailingCountry));
        AddSet(sets, commandParameters, "account_status", Trim(request.AccountStatus));
        AddSet(sets, commandParameters, "account_source", Trim(request.AccountSource));
        AddSet(sets, commandParameters, "hotlisted", request.Hotlisted);
        sets.Add("updated_at = now()");
        var sql = $"update public.sa_accounts set {string.Join(", ", sets)} where id = @id";
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);
            foreach (var parameter in commandParameters)
            {
                command.Parameters.Add(parameter);
            }
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            AppLog.Info($"PostgreSQL UPDATE public.sa_accounts PUT /accounts/{{id}} rows={affected}");
            return affected > 0;
        }
        catch (Exception ex)
        {
            AppLog.Error("PostgreSQL UPDATE public.sa_accounts PUT /accounts/{id} failed", ex);
            throw;
        }
    }

    private static DbAccountRow ReadRow(NpgsqlDataReader reader)
    {
        return new DbAccountRow
        {
            Id = reader.GetGuid(0),
            Salutation = reader.IsDBNull(1) ? null : reader.GetString(1),
            FirstName = reader.IsDBNull(2) ? null : reader.GetString(2),
            LastName = reader.GetString(3),
            PersonEmail = reader.GetString(4),
            PersonBirthdate = reader.IsDBNull(5) ? null : DateOnly.FromDateTime(reader.GetDateTime(5)),
            Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
            MobilePhone = reader.IsDBNull(7) ? null : reader.GetString(7),
            MailingStreet = reader.IsDBNull(8) ? null : reader.GetString(8),
            MailingPostalCode = reader.IsDBNull(9) ? null : reader.GetString(9),
            MailingCity = reader.IsDBNull(10) ? null : reader.GetString(10),
            MailingCountry = reader.IsDBNull(11) ? null : reader.GetString(11),
            AccountStatus = reader.IsDBNull(12) ? null : reader.GetString(12),
            AccountSource = reader.IsDBNull(13) ? null : reader.GetString(13),
            Hotlisted = reader.GetBoolean(14)
        };
    }

    private static void AddNullable(NpgsqlCommand command, string name, string? value)
    {
        command.Parameters.AddWithValue(name, (object?)value ?? DBNull.Value);
    }

    private static void AddNullable(NpgsqlCommand command, string name, DateOnly? value)
    {
        command.Parameters.AddWithValue(name, value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
    }

    private static void AddNullable(NpgsqlCommand command, string name, bool? value)
    {
        command.Parameters.AddWithValue(name, (object?)value ?? DBNull.Value);
    }

    private static void AddSet(List<string> sets, List<NpgsqlParameter> parameters, string column, string? value)
    {
        if (value is null)
        {
            return;
        }
        var parameterName = column;
        sets.Add($"{column} = @{parameterName}");
        parameters.Add(new NpgsqlParameter(parameterName, value));
    }

    private static void AddSet(List<string> sets, List<NpgsqlParameter> parameters, string column, DateOnly? value)
    {
        if (!value.HasValue)
        {
            return;
        }
        var parameterName = column;
        sets.Add($"{column} = @{parameterName}");
        parameters.Add(new NpgsqlParameter(parameterName, value.Value.ToDateTime(TimeOnly.MinValue)));
    }

    private static void AddSet(List<string> sets, List<NpgsqlParameter> parameters, string column, bool? value)
    {
        if (!value.HasValue)
        {
            return;
        }
        var parameterName = column;
        sets.Add($"{column} = @{parameterName}");
        parameters.Add(new NpgsqlParameter(parameterName, value.Value));
    }

    private static string? Trim(string? value) => value is null ? null : value.Trim();
}
