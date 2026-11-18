using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;
using System.Text;

namespace BusTravelAccountsSaFunction;

public sealed class AccountsRepository(NpgsqlDataSource dataSource, ILogger<AccountsRepository> logger) : IAccountsRepository
{
    public async Task<IReadOnlyList<AccountRecord>> GetByEmailAsync(string? email, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    id AS Id,
    salutation AS Salutation,
    first_name AS FirstName,
    last_name AS LastName,
    person_email AS PersonEmail,
    person_birthdate AS PersonBirthdate,
    phone AS Phone,
    mobile_phone AS MobilePhone,
    mailing_street AS MailingStreet,
    mailing_postal_code AS MailingPostalCode,
    mailing_city AS MailingCity,
    mailing_country AS MailingCountry,
    account_status AS AccountStatus,
    account_source AS AccountSource,
    hotlisted AS Hotlisted
FROM public.sa_accounts
WHERE lower(person_email) = lower(@email)";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<AccountRecord>(new CommandDefinition(sql, new { email }, cancellationToken: cancellationToken))).AsList();
        logger.LogInformation("SELECT public.sa_accounts by email outcome: {Rows} rows", rows.Count);
        return rows;
    }

    public async Task<AccountRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    id AS Id,
    salutation AS Salutation,
    first_name AS FirstName,
    last_name AS LastName,
    person_email AS PersonEmail,
    person_birthdate AS PersonBirthdate,
    phone AS Phone,
    mobile_phone AS MobilePhone,
    mailing_street AS MailingStreet,
    mailing_postal_code AS MailingPostalCode,
    mailing_city AS MailingCity,
    mailing_country AS MailingCountry,
    account_status AS AccountStatus,
    account_source AS AccountSource,
    hotlisted AS Hotlisted
FROM public.sa_accounts
WHERE id = @id";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<AccountRecord>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
        logger.LogInformation("SELECT public.sa_accounts by id outcome: {Found}", row is not null ? 1 : 0);
        return row;
    }

    public async Task<Guid> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var columns = new List<string> { "id", "last_name", "first_name", "salutation", "person_mailing_street" };
        var values = new List<string>();
        var parameters = new DynamicParameters();

        columns = new List<string> { "id", "last_name", "first_name", "salutation", "person_email", "mailing_street", "mailing_city", "mailing_postal_code", "mailing_country", "account_status", "account_source" };
        values.Add("@id");
        values.Add("@last_name");
        values.Add("@first_name");
        values.Add("@salutation");
        values.Add("@person_email");
        values.Add("@mailing_street");
        values.Add("@mailing_city");
        values.Add("@mailing_postal_code");
        values.Add("@mailing_country");
        values.Add("@account_status");
        values.Add("@account_source");

        parameters.Add("id", id);
        parameters.Add("last_name", TrimOrNull(request.LastName));
        parameters.Add("first_name", TrimOrNull(request.FirstName));
        parameters.Add("salutation", TrimOrNull(request.Salutation));
        parameters.Add("person_email", TrimOrNull(request.PersonEmail));
        parameters.Add("mailing_street", TrimOrNull(request.MailingStreet));
        parameters.Add("mailing_city", TrimOrNull(request.MailingCity));
        parameters.Add("mailing_postal_code", TrimOrNull(request.MailingPostalCode));
        parameters.Add("mailing_country", TrimOrNull(request.MailingCountry));
        parameters.Add("account_status", TrimOrNull(request.AccountStatus));
        parameters.Add("account_source", TrimOrNull(request.AccountSource));

        if (request.Phone is not null)
        {
            columns.Add("phone");
            values.Add("@phone");
            parameters.Add("phone", request.Phone.Trim());
        }

        if (request.MobilePhone is not null)
        {
            columns.Add("mobile_phone");
            values.Add("@mobile_phone");
            parameters.Add("mobile_phone", request.MobilePhone.Trim());
        }

        if (request.PersonBirthdate is not null)
        {
            columns.Add("person_birthdate");
            values.Add("@person_birthdate");
            parameters.Add("person_birthdate", request.PersonBirthdate);
        }

        var sql = $@"
INSERT INTO public.sa_accounts ({string.Join(", ", columns)})
VALUES ({string.Join(", ", values)})";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        logger.LogInformation("INSERT public.sa_accounts outcome: {Rows} rows", rows);
        return id;
    }

    public async Task<Guid> UpdateAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        var sets = new List<string>
        {
            "last_name = @last_name",
            "first_name = @first_name",
            "salutation = @salutation",
            "person_email = @person_email",
            "mailing_street = @mailing_street",
            "mailing_city = @mailing_city",
            "mailing_postal_code = @mailing_postal_code",
            "mailing_country = @mailing_country",
            "account_status = @account_status",
            "account_source = @account_source",
            "updated_at = now()"
        };

        var parameters = new DynamicParameters();
        parameters.Add("id", id);
        parameters.Add("last_name", TrimOrNull(request.LastName));
        parameters.Add("first_name", TrimOrNull(request.FirstName));
        parameters.Add("salutation", request.Salutation);
        parameters.Add("person_email", TrimOrNull(request.PersonEmail));
        parameters.Add("mailing_street", TrimOrNull(request.MailingStreet));
        parameters.Add("mailing_city", TrimOrNull(request.MailingCity));
        parameters.Add("mailing_postal_code", TrimOrNull(request.MailingPostalCode));
        parameters.Add("mailing_country", TrimOrNull(request.MailingCountry));
        parameters.Add("account_status", TrimOrNull(request.AccountStatus));
        parameters.Add("account_source", TrimOrNull(request.AccountSource));

        if (request.Phone is not null)
        {
            sets.Add("phone = @phone");
            parameters.Add("phone", request.Phone.Trim());
        }

        if (request.MobilePhone is not null)
        {
            sets.Add("mobile_phone = @mobile_phone");
            parameters.Add("mobile_phone", request.MobilePhone.Trim());
        }

        if (request.PersonBirthdate is not null)
        {
            sets.Add("person_birthdate = @person_birthdate");
            parameters.Add("person_birthdate", request.PersonBirthdate);
        }

        var sql = $@"
UPDATE public.sa_accounts
SET {string.Join(", ", sets)}
WHERE id = @id";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        logger.LogInformation("UPDATE public.sa_accounts outcome: {Rows} rows", rows);
        return id;
    }

    public async Task CheckHealthAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT 1";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        logger.LogInformation("SELECT health public.sa_accounts outcome: {Rows} rows", 1);
    }

    private static string? TrimOrNull(string? value)
    {
        return value?.Trim();
    }
}