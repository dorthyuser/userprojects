using System.Text.Json.Serialization;

namespace BusTravelAccountsSaLambda;

public sealed class AccountResponse
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("salutation")] public string? Salutation { get; set; }
    [JsonPropertyName("firstName")] public string? FirstName { get; set; }
    [JsonPropertyName("lastName")] public string? LastName { get; set; }
    [JsonPropertyName("personBirthdate")] public DateOnly? PersonBirthdate { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
    [JsonPropertyName("mobilePhone")] public string? MobilePhone { get; set; }
    [JsonPropertyName("personEmail")] public string? PersonEmail { get; set; }
    [JsonPropertyName("mailingStreet")] public string? MailingStreet { get; set; }
    [JsonPropertyName("mailingPostalCode")] public string? MailingPostalCode { get; set; }
    [JsonPropertyName("mailingCity")] public string? MailingCity { get; set; }
    [JsonPropertyName("mailingCountry")] public string? MailingCountry { get; set; }
    [JsonPropertyName("accountStatus")] public string? AccountStatus { get; set; }
    [JsonPropertyName("accountSource")] public string? AccountSource { get; set; }
    [JsonPropertyName("hotlisted")] public bool? Hotlisted { get; set; }

    public static AccountResponse FromRow(AccountRow row) => new()
    {
        Id = row.Id.ToString(),
        Salutation = row.Salutation,
        FirstName = row.FirstName,
        LastName = row.LastName,
        PersonBirthdate = row.PersonBirthdate,
        Phone = row.Phone,
        MobilePhone = row.MobilePhone,
        PersonEmail = row.PersonEmail,
        MailingStreet = row.MailingStreet,
        MailingPostalCode = row.MailingPostalCode,
        MailingCity = row.MailingCity,
        MailingCountry = row.MailingCountry,
        AccountStatus = row.AccountStatus,
        AccountSource = row.AccountSource,
        Hotlisted = row.Hotlisted
    };
}
