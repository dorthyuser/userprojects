namespace BusTravelAccountsLambda;

public sealed class EaSettings
{
    private static readonly Lazy<EaSettings> Lazy = new(Create);
    public static EaSettings Instance => Lazy.Value;
    public string ListenerPath { get; }

    private EaSettings(string listenerPath)
    {
        ListenerPath = listenerPath;
    }

    private static EaSettings Create()
    {
        var listenerPath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH");
        if (string.IsNullOrWhiteSpace(listenerPath))
        {
            listenerPath = "api/v1/*";
        }

        return new EaSettings(listenerPath);
    }
}