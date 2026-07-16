namespace Csharpae1012Lambda.Models;

using Csharpae1012Lambda;

public sealed class PostSuccessResponse
{
    public string Status { get; set; } = "success";
    public string AeId { get; set; } = string.Empty;
    public string NotificationId { get; set; } = string.Empty;
    public bool SnsPublished { get; set; }
    public string? SnsMessageId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
}

public sealed class NotificationItem
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
    public string? AcknowledgedBy { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public bool SnsPublished { get; set; }
    public string? SnsMessageId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class GetSuccessResponse
{
    public string Status { get; set; } = "success";
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<NotificationItem> Notifications { get; set; } = new();
}

public sealed class ErrorResponse
{
    public string Status { get; set; } = "error";
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? AeId { get; set; }
}