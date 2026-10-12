using AdverseEventNotificationService.Api.Domain;

namespace AdverseEventNotificationService.Api.Contracts;

public sealed record AdverseEventNotificationItemDto(
    string NotificationId,
    string AeId,
    string TrialId,
    string SiteId,
    string PatientId,
    string AeTermName,
    int CtcaeGrade,
    bool Serious,
    AdverseEventPriority Priority,
    AdverseEventOutcome Outcome,
    bool Acknowledged,
    bool SnsPublished,
    DateTimeOffset CreatedAt);