namespace PrimeNumberService.Configuration;

public sealed class DatabaseOptions
{
    public string SearchPath { get; set; } = "public";
    public int ConnectTimeoutSeconds { get; set; } = 5;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int MinPoolSize { get; set; } = 2;
    public int MaxPoolSize { get; set; } = 20;
}
