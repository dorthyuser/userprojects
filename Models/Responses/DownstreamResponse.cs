namespace BusTravelAccountsEaMainFunction;

public sealed class DownstreamResponse
{
    public int StatusCode { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? ContentType { get; set; }
}