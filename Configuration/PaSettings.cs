namespace MulecombineMainLambda;

public sealed class PaSettings
{
    private static readonly Lazy<PaSettings> CurrentLazy = new(() => new PaSettings());

    public string ListenerPath { get; }

    public static PaSettings Current => CurrentLazy.Value;

    private PaSettings()
    {
        ListenerPath = PaPathHelper.NormalizeBasePath(Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH"));
        if (string.IsNullOrWhiteSpace(ListenerPath))
        {
            ListenerPath = "api/v1";
        }
    }

    public static string ResolveListenerPath() => Current.ListenerPath;
}