using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BusTravelAccountsSaFunction;

public sealed class AccountsRepository(
    NpgsqlDataSource dataSource,
    IOptions<DatabaseOptions> options,
    ILogger<AccountsRepository> logger) : IAccountsRepository
{
    private readonly int _commandTimeoutSeconds = Math.Max(1, options.Value.ResponseTimeoutMilliseconds / 1000);

    public async Task<IReadOnlyList<AccountResponse>> GetByEmailAsync(string? email, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    id AS Id,
    salutation AS Salutation,
    first_name AS FirstName,
    last_name AS LastName,
    person_birthdate AS PersonBirthdate,
    phone AS Phone,
    mobile_phone AS MobilePhone,
    person_email AS PersonEmail,
    mailing_street AS MailingStreet,
    mailing_postal_code AS MailingPostalCode,
    mailing_city AS MailingCity,
    mailing_country AS MailingCountry,
    account_status AS AccountStatus,
    COALESCE(account_source, '') AS AccountSource,
    hotlisted AS Hotlisted
FROM public.sa_accounts
WHERE lower(person_email) = lower(@email)";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<AccountResponse>(new CommandDefinition(
            sql,
            new { email },
            commandTimeout: _commandTimeoutSeconds,
            cancellationToken: cancellationToken))).AsList();

        logger.LogInformation("SELECT public.sa_accounts by email outcome: {Rows} rows", rows.Count);
        return rows;
    }

    public async Task<AccountResponse?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    id AS Id,
    salutation AS Salutation,
    first_name AS FirstName,
    last_name AS LastName,
    person_birthdate AS PersonBirthdate,
    phone AS Phone,
    mobile_phone AS MobilePhone,
    person_email AS PersonEmail,
    mailing_street AS MailingStreet,
    mailing_postal_code AS MailingPostalCode,
    mailing_city AS MailingCity,
    mailing_country AS MailingCountry,
    account_status AS AccountStatus,
    COALESCE(account_source, '') AS AccountSource,
    hotlisted AS Hotlisted
FROM public.sa_accounts
WHERE id::text = @id";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<AccountResponse>(new CommandDefinition(
            sql,
            new { id },
            commandTimeout: _commandTimeoutSeconds,
            cancellationToken: cancellationToken));

        logger.LogInformation("SELECT public.sa_accounts by id outcome: {Found}", row is not null ? 1 : 0);
        return row;
    }

    public async Task<string> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var columns = new List<string>
        {
            "id",
            "last_name",
            "first_name",
            "salutation",
            "person_email",
            "mailing_street",
            "mailing_city",
            "mailing_postal_code",
            "mailing_country",
            "account_status",
            "account_source"
        };

        var values = new List<string>
        {
            "@id",
            "@last_name",
            "@first_name",
            "@salutation",
            "@person_email",
            "@mailing_street",
            "@mailing_city",
            "@mailing_postal_code",
            "@mailing_country",
            "@account_status",
            "@account_source"
        };

        var parameters = new DynamicParameters();
        parameters.Add("id", id);
        parameters.Add("last_name", StringTrimHelper.TrimOrNull(request.LastName));
        parameters.Add("first_name", StringTrimHelper.TrimOrNull(request.FirstName));
        parameters.Add("salutation", StringTrimHelper.TrimOrNull(request.Salutation));
        parameters.Add("person_email", StringTrimHelper.TrimOrNull(request.PersonEmail));
        parameters.Add("mailing_street", StringTrimHelper.TrimOrNull(request.MailingStreet));
        parameters.Add("mailing_city", StringTrimHelper.TrimOrNull(request.MailingCity));
        parameters.Add("mailing_postal_code", StringTrimHelper.TrimOrNull(request.MailingPostalCode));
        parameters.Add("mailing_country", StringTrimHelper.TrimOrNull(request.MailingCountry));
        parameters.Add("account_status", StringTrimHelper.TrimOrNull(request.AccountStatus));
        parameters.Add("account_source", StringTrimHelper.TrimOrNull(request.AccountSource));

        if (request.Phone is not null)
        {
            columns.Add("phone");
            values.Add("@phone");
            parameters.Add("phone", StringTrimHelper.TrimOrNull(request.Phone));
        }

        if (request.MobilePhone is not null)
        {
            columns.Add("mobile_phone");
            values.Add("@mobile_phone");
            parameters.Add("mobile_phone", StringTrimHelper.TrimOrNull(request.MobilePhone));
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
        var rows = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _commandTimeoutSeconds,
            cancellationToken: cancellationToken));

        logger.LogInformation("INSERT public.sa_accounts outcome: {RowsAffected} rows", rows);
        return id.ToString();
    }

    public async Task<string> UpdateAsync(string id, UpdateAccountRequest request, CancellationToken cancellationToken)
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
        parameters.Add("last_name", StringTrimHelper.TrimOrNull(request.LastName));
        parameters.Add("first_name", StringTrimHelper.TrimOrNull(request.FirstName));
        parameters.Add("salutation", request.Salutation);
        parameters.Add("person_email", StringTrimHelper.TrimOrNull(request.PersonEmail));
        parameters.Add("mailing_street", StringTrimHelper.TrimOrNull(request.MailingStreet));
        parameters.Add("mailing_city", StringTrimHelper.TrimOrNull(request.MailingCity));
        parameters.Add("mailing_postal_code", StringTrimHelper.TrimOrNull(request.MailingPostalCode));
        parameters.Add("mailing_country", StringTrimHelper.TrimOrNull(request.MailingCountry));
        parameters.Add("account_status", StringTrimHelper.TrimOrNull(request.AccountStatus));
        parameters.Add("account_source", StringTrimHelper.TrimOrNull(request.AccountSource));

        if (request.Phone is not null)
        {
            sets.Add("phone = @phone");
            parameters.Add("phone", StringTrimHelper.TrimOrNull(request.Phone));
        }

        if (request.MobilePhone is not null)
        {
            sets.Add("mobile_phone = @mobile_phone");
            parameters.Add("mobile_phone", StringTrimHelper.TrimOrNull(request.MobilePhone));
        }

        if (request.PersonBirthdate is not null)
        {
            sets.Add("person_birthdate = @person_birthdate");
            parameters.Add("person_birthdate", request.PersonBirthdate);
        }

        var sqlBuilder = new StringBuilder();
        sqlBuilder.Append("UPDATE public.sa_accounts SET ");
        sqlBuilder.Append(string.Join(", ", sets));
        sqlBuilder.Append(" WHERE id::text = @id");

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(new CommandDefinition(
            sqlBuilder.ToString(),
            parameters,
            commandTimeout: _commandTimeoutSeconds,
            cancellationToken: cancellationToken));

        logger.LogInformation("UPDATE public.sa_accounts outcome: {RowsAffected} rows", rows);
        return id;
    }
}