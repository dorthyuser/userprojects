namespace BusTravelAccountsSaLambda;

public sealed class DbAccountRow
{
    public Guid Id { get; set; }
    public string? Salutation { get; set; }
    public string? FirstName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string PersonEmail { get; set; } = string.Empty;
    public DateOnly? PersonBirthdate { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? MailingStreet { get; set; }
    public string? MailingPostalCode { get; set; }
    public string? MailingCity { get; set; }
    public string? MailingCountry { get; set; }
    public string? AccountStatus { get; set; }
    public string? AccountSource { get; set; }
    public bool Hotlisted { get; set; }
}
