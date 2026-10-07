namespace MulecombineMainLambda;

public sealed class PaApiResult
{
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}