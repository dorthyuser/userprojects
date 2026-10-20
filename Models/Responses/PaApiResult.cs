namespace BusTravelAccountsLambda;

public sealed class PaApiResult
{
    public int StatusCode { get; init; }
    public string? Body { get; init; }
    public IDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
