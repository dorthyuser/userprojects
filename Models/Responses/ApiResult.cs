using System.Collections.Generic;

namespace BusTravelAccountsSaLambda;

public sealed class ApiResult
{
    public int StatusCode { get; init; }
    public string? Body { get; init; }
    public IDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public static ApiResult Text(int statusCode, string body) => new() { StatusCode = statusCode, Body = body, Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "text/plain" } };
    public static ApiResult Json(int statusCode, string body) => new() { StatusCode = statusCode, Body = body, Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" } };
}
