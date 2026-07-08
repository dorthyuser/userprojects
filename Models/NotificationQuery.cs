using System;

namespace AdverseEventReporter.Models;

public class NotificationQuery
{
    public string? TrialId { get; set; }
    public string? SiteId { get; set; }
    public int? CtcaeGrade { get; set; }
    public bool? Serious { get; set; }
    public bool? Acknowledged { get; set; }
    public string? Priority { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
