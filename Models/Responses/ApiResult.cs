namespace BusTravelAccountsSaLambda;

public sealed class ApiResult
{
    public int StatusCode { get; init; }
    public string? Body { get; init; }
    public IDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}