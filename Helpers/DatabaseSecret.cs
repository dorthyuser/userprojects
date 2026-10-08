namespace BusTravelAccountsSaMainLambda;

public sealed class DatabaseSecret
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string DbName { get; set; } = string.Empty;
}