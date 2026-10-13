using System.Data;
using Npgsql;

namespace BusTravelAccountsSaLambda;

public sealed class AccountRow
{
    public Guid Id { get; set; }
    public string? Salutation { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? PersonBirthdate { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? PersonEmail { get; set; }
    public string? MailingStreet { get; set; }
    public string? MailingPostalCode { get; set; }
    public string? MailingCity { get; set; }
    public string? MailingCountry { get; set; }
    public string? AccountStatus { get; set; }
    public string? AccountSource { get; set; }
    public bool? Hotlisted { get; set; }

    public static AccountRow FromReader(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("id")),
        Salutation = reader.IsDBNull(reader.GetOrdinal("salutation")) ? null : reader.GetString(reader.GetOrdinal("salutation")),
        FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString(reader.GetOrdinal("first_name")),
        LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString(reader.GetOrdinal("last_name")),
        PersonBirthdate = reader.IsDBNull(reader.GetOrdinal("person_birthdate")) ? null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("person_birthdate"))),
        Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
        MobilePhone = reader.IsDBNull(reader.GetOrdinal("mobile_phone")) ? null : reader.GetString(reader.GetOrdinal("mobile_phone")),
        PersonEmail = reader.IsDBNull(reader.GetOrdinal("person_email")) ? null : reader.GetString(reader.GetOrdinal("person_email")),
        MailingStreet = reader.IsDBNull(reader.GetOrdinal("mailing_street")) ? null : reader.GetString(reader.GetOrdinal("mailing_street")),
        MailingPostalCode = reader.IsDBNull(reader.GetOrdinal("mailing_postal_code")) ? null : reader.GetString(reader.GetOrdinal("mailing_postal_code")),
        MailingCity = reader.IsDBNull(reader.GetOrdinal("mailing_city")) ? null : reader.GetString(reader.GetOrdinal("mailing_city")),
        MailingCountry = reader.IsDBNull(reader.GetOrdinal("mailing_country")) ? null : reader.GetString(reader.GetOrdinal("mailing_country")),
        AccountStatus = reader.IsDBNull(reader.GetOrdinal("account_status")) ? null : reader.GetString(reader.GetOrdinal("account_status")),
        AccountSource = reader.IsDBNull(reader.GetOrdinal("account_source")) ? null : reader.GetString(reader.GetOrdinal("account_source")),
        Hotlisted = reader.IsDBNull(reader.GetOrdinal("hotlisted")) ? null : reader.GetBoolean(reader.GetOrdinal("hotlisted"))
    };
}
