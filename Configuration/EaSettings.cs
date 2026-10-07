namespace MuleaesaMainLambda;

public sealed class EaSettings
{
    private static readonly Lazy<EaSettings> Instance = new(Create);

    public string ListenerPath { get; }

    private EaSettings(string listenerPath)
    {
        ListenerPath = listenerPath;
    }

    public static EaSettings Current() => Instance.Value;

    private static EaSettings Create()
    {
        var listenerPath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH") ?? "api/v1/*";
        return new EaSettings(listenerPath);
    }
}