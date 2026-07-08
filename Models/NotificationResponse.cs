using System.Collections.Generic;

namespace AdverseEventReporter.Models;

public class NotificationResponse
{
    public string Status { get; set; } = "success";
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<NotificationItem> Notifications { get; set; } = new();
}
