using System.ComponentModel.DataAnnotations;

namespace BusTravelAccountsSaFunction;

public sealed class SalesforceOptions
{
    public const string Section = "Salesforce";

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string LoginUrl { get; set; } = string.Empty;

    [Required]
    public string ConsumerKey { get; set; } = string.Empty;

    [Required]
    public string PrivateKey { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ResponseTimeoutMilliseconds { get; set; }

    [Range(1, int.MaxValue)]
    public int ConnectTimeoutMilliseconds { get; set; }
}
