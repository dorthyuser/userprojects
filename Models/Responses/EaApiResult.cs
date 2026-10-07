namespace MuleaesaMainLambda;

public sealed class EaApiResult
{
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
