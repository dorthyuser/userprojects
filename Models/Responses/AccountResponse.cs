using System.Text.Json.Serialization;
namespace BusTravelAccountsSaMainLambda;

public sealed class AccountResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("salutation")]
    public string? Salutation { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("personEmail")]
    public string? PersonEmail { get; set; }

    [JsonPropertyName("personBirthdate")]
    public DateOnly? PersonBirthdate { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("mobilePhone")]
    public string? MobilePhone { get; set; }

    [JsonPropertyName("mailingStreet")]
    public string? MailingStreet { get; set; }

    [JsonPropertyName("mailingPostalCode")]
    public string? MailingPostalCode { get; set; }

    [JsonPropertyName("mailingCity")]
    public string? MailingCity { get; set; }

    [JsonPropertyName("mailingCountry")]
    public string? MailingCountry { get; set; }

    [JsonPropertyName("accountStatus")]
    public string? AccountStatus { get; set; }

    [JsonPropertyName("accountSource")]
    public string? AccountSource { get; set; }

    [JsonPropertyName("hotlisted")]
    public bool? Hotlisted { get; set; }
}
