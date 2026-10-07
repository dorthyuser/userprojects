using System.Data;
using Npgsql;

namespace MuleaesaMainLambda;

public sealed class SaDatabaseClient
{
    private static readonly NpgsqlDataSource DataSource = NpgsqlDataSource.Create(SaSettings.Current().ConnectionString);

    public async Task<IReadOnlyList<SaAccountResponse>> GetAccountsByEmailAsync(string? email)
    {
        var results = new List<SaAccountResponse>();
        await using var command = DataSource.CreateCommand(@"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where ($1 is null or lower(person_email) = lower($1)) order by created_at desc");
        command.Parameters.AddWithValue(email is null ? DBNull.Value : email);
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            results.Add(ReadAccount(reader));
        }

        return results;
    }

    public async Task<SaAccountResponse?> GetAccountByIdAsync(Guid id)
    {
        await using var command = DataSource.CreateCommand(@"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = $1");
        command.Parameters.AddWithValue(id);
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        if (!await reader.ReadAsync().ConfigureAwait(false))
        {
            return null;
        }

        return ReadAccount(reader);
    }

    public async Task InsertAccountAsync(Guid id, SaAccountRequest request)
    {
        await using var command = DataSource.CreateCommand(@"insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, created_at, updated_at) values ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14,$15, now(), now())");
        AddParameters(command, id, request, includeId: true);
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task<bool> UpdateAccountAsync(Guid id, SaAccountRequest request)
    {
        await using var command = DataSource.CreateCommand(@"update public.sa_accounts set salutation = $2, first_name = $3, last_name = $4, person_email = $5, person_birthdate = $6, phone = $7, mobile_phone = $8, mailing_street = $9, mailing_postal_code = $10, mailing_city = $11, mailing_country = $12, account_status = $13, account_source = $14, hotlisted = $15, updated_at = now() where id = $1");
        AddParameters(command, id, request, includeId: true);
        var rows = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        return rows > 0;
    }

    private static void AddParameters(NpgsqlCommand command, Guid id, SaAccountRequest request, bool includeId)
    {
        if (includeId)
        {
            command.Parameters.AddWithValue(id);
        }

        command.Parameters.AddWithValue((object?)request.Salutation ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.FirstName ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.LastName ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.PersonEmail ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.PersonBirthdate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.MobilePhone ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.MailingStreet ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.MailingPostalCode ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.MailingCity ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.MailingCountry ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.AccountStatus ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.AccountSource ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)request.Hotlisted ?? DBNull.Value);
    }

    private static SaAccountResponse ReadAccount(NpgsqlDataReader reader)
    {
        return new SaAccountResponse
        {
            Id = reader.GetGuid(0),
            Salutation = reader.IsDBNull(1) ? null : reader.GetString(1),
            FirstName = reader.IsDBNull(2) ? null : reader.GetString(2),
            LastName = reader.IsDBNull(3) ? null : reader.GetString(3),
            PersonBirthdate = reader.IsDBNull(5) ? null : DateOnly.FromDateTime(reader.GetDateTime(5)),
            Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
            MobilePhone = reader.IsDBNull(7) ? null : reader.GetString(7),
            PersonEmail = reader.IsDBNull(4) ? null : reader.GetString(4),
            MailingStreet = reader.IsDBNull(8) ? null : reader.GetString(8),
            MailingPostalCode = reader.IsDBNull(9) ? null : reader.GetString(9),
            MailingCity = reader.IsDBNull(10) ? null : reader.GetString(10),
            MailingCountry = reader.IsDBNull(11) ? null : reader.GetString(11),
            AccountStatus = reader.IsDBNull(12) ? null : reader.GetString(12),
            AccountSource = reader.IsDBNull(13) ? null : reader.GetString(13),
            Hotlisted = reader.IsDBNull(14) ? null : reader.GetBoolean(14)
        };
    }
}
