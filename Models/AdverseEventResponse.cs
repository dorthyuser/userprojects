using System;

namespace AdverseEventReporter.Models;

public class AdverseEventResponse
{
    public string Status { get; set; } = "success";
    public string AeId { get; set; } = string.Empty;
    public string NotificationId { get; set; } = string.Empty;
    public bool SnsPublished { get; set; }
    public string? SnsMessageId { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}
