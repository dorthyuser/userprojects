namespace BusTravelAccountsLambda;

public sealed class PaSettings
{
    private static readonly Lazy<PaSettings> Lazy = new(() => new PaSettings());
    public static PaSettings Instance => Lazy.Value;
    private PaSettings() { }
}