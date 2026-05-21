namespace synctesting1109.Models;

public sealed class ApiResult
{
    public int StatusCode { get; set; }
    public object? Body { get; set; }
    public string? ContentType { get; set; }
}