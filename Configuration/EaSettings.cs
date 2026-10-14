namespace BusTravelAccountsLambda;

public sealed class EaSettings
{
    public string ListenerPath { get; }

    private EaSettings(string listenerPath)
    {
        ListenerPath = listenerPath;
    }

    public static EaSettings Current()
    {
        var listenerPath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH");
        return new EaSettings(string.IsNullOrWhiteSpace(listenerPath) ? "api/v1/*" : listenerPath);
    }
}