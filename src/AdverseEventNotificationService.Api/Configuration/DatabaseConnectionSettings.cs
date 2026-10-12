namespace AdverseEventNotificationService.Api.Configuration;

public sealed record DatabaseConnectionSettings(
    string Host,
    int Port,
    string Database,
    string Username,
    string Password);