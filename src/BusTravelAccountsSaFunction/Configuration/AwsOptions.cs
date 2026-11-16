using System.ComponentModel.DataAnnotations;

namespace BusTravelAccountsSaFunction;

public sealed class AwsOptions
{
    public const string Section = "Aws";

    [Required]
    public string AccessKeyId { get; set; } = string.Empty;

    [Required]
    public string SecretAccessKey { get; set; } = string.Empty;

    [Required]
    public string Region { get; set; } = string.Empty;

    [Required]
    public string Bucket { get; set; } = string.Empty;
}
