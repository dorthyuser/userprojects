namespace test_sf_git_prop.Models;

public sealed class SalesforceOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 8080;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SecurityToken { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v59.0";
    public string InstanceUrl { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public bool UseSandbox { get; set; } = true;
    public string ProductionLoginUrl { get; set; } = "https://login.salesforce.com";
    public string SandboxLoginUrl { get; set; } = "https://test.salesforce.com";
}