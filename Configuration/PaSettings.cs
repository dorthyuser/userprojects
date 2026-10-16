namespace BusTravelAccountsLambda;

public sealed class PaSettings
{
    private static readonly Lazy<PaSettings> LazyInstance = new(() => new PaSettings());
    public static PaSettings Instance => LazyInstance.Value;
    private PaSettings() { }
}