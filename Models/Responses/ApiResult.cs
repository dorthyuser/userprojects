namespace BusTravelAccountsSaMainLambda;

public sealed class ApiResult
{
    public ApiResult(int statusCode, string? body)
    {
        StatusCode = statusCode;
        Body = body;
        Headers = ResponseHelper.CreateHeaders();
    }

    public int StatusCode { get; }
    public string? Body { get; }
    public IDictionary<string, string> Headers { get; }
}