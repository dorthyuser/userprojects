namespace Aelambda1024Lambda.Models;

public sealed class AdverseEventResponse
{
    public string Status { get; set; } = "success";
    public string AeId { get; set; } = string.Empty;
    public string NotificationId { get; set; } = string.Empty;
    public bool SnsPublished { get; set; }
    public string? SnsMessageId { get; set; }
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
    public NotificationPriority Priority { get; set; }
    public AeOutcome Outcome { get; set; }
    public bool Acknowledged { get; set; }
    public bool SnsPublished { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class NotificationsResponse
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
    public string Code { get; }
    public string Message { get; }
    public string? ExistingAeId { get; }

    public ErrorResponse(string code, string message, string? existingAeId = null)
    {
        Code = code;
        Message = message;
        ExistingAeId = existingAeId;
    }
}