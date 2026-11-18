using System.ComponentModel.DataAnnotations;

namespace BusTravelAccountsSaFunction;

public sealed class DatabaseOptions
{
    public const string Section = "Database";

    public string? KeyVault { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Host { get; set; }

    public string? Dbname { get; set; }

    public string? Port { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int ResponseTimeoutMilliseconds { get; set; }
}