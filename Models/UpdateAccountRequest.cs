namespace test_sf_git_prop.Models;

public sealed class UpdateAccountRequest
{
    public string? Name { get; set; }
    public string? AccountNumber { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? BillingStreet { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCountry { get; set; }
}