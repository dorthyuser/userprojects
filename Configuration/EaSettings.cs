namespace MulecombineMainLambda;

public sealed class EaSettings
{
    private static readonly Lazy<EaSettings> LazyCurrent = new(() => new EaSettings());

    public string ListenerPath { get; }

    public static EaSettings Current => LazyCurrent.Value;

    private EaSettings()
    {
        ListenerPath = PathHelper.NormalizeBasePath(Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH"));
    }
}