namespace PrimeNumberService.Configuration;

public sealed class ServiceOptions
{
    public const string SectionName = "Service";

    public string Name { get; set; } = "PrimeNumberService";
    public string DefaultRegion { get; set; } = "eu-west-2";
    public string SecretsManagerSecretName { get; set; } = string.Empty;
    public DatabaseOptions Database { get; set; } = new();
}
