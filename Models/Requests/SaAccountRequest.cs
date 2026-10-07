namespace MuleaesaMainLambda;

public sealed class SaAccountRequest
{
    public string? Salutation { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PersonEmail { get; set; }
    public DateOnly? PersonBirthdate { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? MailingStreet { get; set; }
    public string? MailingPostalCode { get; set; }
    public string? MailingCity { get; set; }
    public string? MailingCountry { get; set; }
    public string? AccountStatus { get; set; }
    public string? AccountSource { get; set; }
    public bool? Hotlisted { get; set; }
}