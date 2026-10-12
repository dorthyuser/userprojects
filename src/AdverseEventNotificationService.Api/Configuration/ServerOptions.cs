namespace AdverseEventNotificationService.Api.Configuration;

public sealed class ServerOptions
{
    public const string SectionName = "Server";

    public int Port { get; init; } = 8080;
}