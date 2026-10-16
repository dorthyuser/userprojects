namespace Azfcsharp8Function;

public sealed class ErrorResponse
{
    public ErrorDetail? error { get; set; }
}

public sealed class ErrorDetail
{
    public int errorCode { get; set; }
    public DateTime errorDateTime { get; set; }
    public string? errorMessage { get; set; }
    public string? errorDescription { get; set; }
}
