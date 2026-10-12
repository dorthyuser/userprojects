namespace AdverseEventNotificationService.Api.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public int CommandTimeoutSeconds { get; init; } = 30;
    public int MaxPageSize { get; init; } = 100;
}