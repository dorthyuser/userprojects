using System.Collections.Generic;

namespace Azfcsharp8Function;

public sealed class ApiResult
{
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}
