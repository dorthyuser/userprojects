namespace BusTravelAccountsLambda;

public sealed class EaSettings
{
    private static readonly Lazy<EaSettings> LazyInstance = new(() => new EaSettings());
    public static EaSettings Instance => LazyInstance.Value;
    private EaSettings() { }
}
