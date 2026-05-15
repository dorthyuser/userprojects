namespace TravelcardDb.Models;

public sealed class TravelcardForwardResult
{
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public string? ContentType { get; set; }
}