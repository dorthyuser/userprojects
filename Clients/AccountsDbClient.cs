using System.Data;
using System.Text.Json;
using Npgsql;
namespace BusTravelAccountsSaMainLambda;

public sealed class AccountsDbClient
{
    private readonly Settings _settings;
    private static readonly JsonSerializerOptions JsonOptions = JsonHelper.CreateOptions();

    public AccountsDbClient(Settings settings)
    {
        _settings = settings;
    }

    public async Task<List<AccountResponse>> GetAccountsAsync(string? email, CancellationToken cancellationToken)
    {
        var results = new List<AccountResponse>();
        await using var connection = new NpgsqlConnection(_settings.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        if (string.IsNullOrWhiteSpace(email))
        {
            command.CommandText = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts order by created_at desc";
        }
        else
        {
            command.CommandText = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(@email) order by created_at desc";
            command.Parameters.AddWithValue("email", email.Trim());
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(ReadAccount(reader));
        }

        return results;
    }

    public async Task<AccountResponse?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_settings.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = @id";
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return ReadAccount(reader);
    }

    public async Task<CreatedAccountResponse> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(_settings.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, created_at, updated_at) values (@id, @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source, @hotlisted, now(), now())";
        AddParameters(command, id, request.Salutation, request.FirstName, request.LastName, request.PersonEmail, request.PersonBirthdate, request.Phone, request.MobilePhone, request.MailingStreet, request.MailingPostalCode, request.MailingCity, request.MailingCountry, request.AccountStatus, request.AccountSource, request.Hotlisted ?? false);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return new CreatedAccountResponse(id.ToString());
    }

    public async Task<bool> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_settings.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"update public.sa_accounts set salutation = @salutation, first_name = @first_name, last_name = @last_name, person_email = @person_email, person_birthdate = @person_birthdate, phone = @phone, mobile_phone = @mobile_phone, mailing_street = @mailing_street, mailing_postal_code = @mailing_postal_code, mailing_city = @mailing_city, mailing_country = @mailing_country, account_status = @account_status, account_source = @account_source, hotlisted = coalesce(@hotlisted, hotlisted), updated_at = now() where id = @id";
        AddParameters(command, id, request.Salutation, request.FirstName, request.LastName, request.PersonEmail, request.PersonBirthdate, request.Phone, request.MobilePhone, request.MailingStreet, request.MailingPostalCode, request.MailingCity, request.MailingCountry, request.AccountStatus, request.AccountSource, request.Hotlisted);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static void AddParameters(NpgsqlCommand command, Guid id, string? salutation, string? firstName, string? lastName, string? personEmail, DateOnly? personBirthdate, string? phone, string? mobilePhone, string? mailingStreet, string? mailingPostalCode, string? mailingCity, string? mailingCountry, string? accountStatus, string? accountSource, bool? hotlisted)
    {
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("salutation", (object?)salutation ?? DBNull.Value);
        command.Parameters.AddWithValue("first_name", (object?)firstName ?? DBNull.Value);
        command.Parameters.AddWithValue("last_name", (object?)lastName ?? DBNull.Value);
        command.Parameters.AddWithValue("person_email", (object?)personEmail ?? DBNull.Value);
        command.Parameters.AddWithValue("person_birthdate", personBirthdate.HasValue ? personBirthdate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("phone", (object?)phone ?? DBNull.Value);
        command.Parameters.AddWithValue("mobile_phone", (object?)mobilePhone ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_street", (object?)mailingStreet ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_postal_code", (object?)mailingPostalCode ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_city", (object?)mailingCity ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_country", (object?)mailingCountry ?? DBNull.Value);
        command.Parameters.AddWithValue("account_status", (object?)accountStatus ?? DBNull.Value);
        command.Parameters.AddWithValue("account_source", (object?)accountSource ?? DBNull.Value);
        command.Parameters.AddWithValue("hotlisted", hotlisted.HasValue ? hotlisted.Value : DBNull.Value);
    }

    private static AccountResponse ReadAccount(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("id")).ToString(),
        Salutation = reader.IsDBNull(reader.GetOrdinal("salutation")) ? null : reader.GetString(reader.GetOrdinal("salutation")),
        FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString(reader.GetOrdinal("first_name")),
        LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString(reader.GetOrdinal("last_name")),
        PersonEmail = reader.IsDBNull(reader.GetOrdinal("person_email")) ? null : reader.GetString(reader.GetOrdinal("person_email")),
        PersonBirthdate = reader.IsDBNull(reader.GetOrdinal("person_birthdate")) ? null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("person_birthdate"))),
        Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
        MobilePhone = reader.IsDBNull(reader.GetOrdinal("mobile_phone")) ? null : reader.GetString(reader.GetOrdinal("mobile_phone")),
        MailingStreet = reader.IsDBNull(reader.GetOrdinal("mailing_street")) ? null : reader.GetString(reader.GetOrdinal("mailing_street")),
        MailingPostalCode = reader.IsDBNull(reader.GetOrdinal("mailing_postal_code")) ? null : reader.GetString(reader.GetOrdinal("mailing_postal_code")),
        MailingCity = reader.IsDBNull(reader.GetOrdinal("mailing_city")) ? null : reader.GetString(reader.GetOrdinal("mailing_city")),
        MailingCountry = reader.IsDBNull(reader.GetOrdinal("mailing_country")) ? null : reader.GetString(reader.GetOrdinal("mailing_country")),
        AccountStatus = reader.IsDBNull(reader.GetOrdinal("account_status")) ? null : reader.GetString(reader.GetOrdinal("account_status")),
        AccountSource = reader.IsDBNull(reader.GetOrdinal("account_source")) ? string.Empty : reader.GetString(reader.GetOrdinal("account_source")),
        Hotlisted = reader.IsDBNull(reader.GetOrdinal("hotlisted")) ? null : reader.GetBoolean(reader.GetOrdinal("hotlisted"))
    };
}
