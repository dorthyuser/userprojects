using System;

namespace AdverseEventReporter.Models;

public class NotificationItem
{
    public string NotificationId { get; set; } = string.Empty;
    public string AeId { get; set; } = string.Empty;
    public string TrialId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string AeTermName { get; set; } = string.Empty;
    public int CtcaeGrade { get; set; }
    public bool Serious { get; set; }
    public Priority Priority { get; set; }
    public Outcome Outcome { get; set; }
    public bool Acknowledged { get; set; }
    public bool SnsPublished { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
