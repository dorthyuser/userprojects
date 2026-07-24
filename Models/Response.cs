namespace Csharpae1012Lambda.Models;

public sealed class Response
{
    public string Status { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Message { get; set; }
    public string? AeId { get; set; }
    public string? NotificationId { get; set; }
    public bool? SnsPublished { get; set; }
    public string? SnsMessageId { get; set; }
    public string? ReceivedAt { get; set; }
    public int? Total { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public List<object>? Notifications { get; set; }
}