using System.ComponentModel.DataAnnotations;
using AdverseEventNotificationService.Api.Domain;

namespace AdverseEventNotificationService.Api.Contracts;

public sealed record NotificationQueryDto
{
    public string? TrialId { get; init; }
    public string? SiteId { get; init; }

    [Range(1, 5)]
    public int? CtcaeGrade { get; init; }

    public bool? Serious { get; init; }
    public bool? Acknowledged { get; init; }
    public AdverseEventPriority? Priority { get; init; }
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo { get; init; }

    [Range(1, int.MaxValue)]
    public int? Page { get; init; }

    [Range(1, 100)]
    public int? PageSize { get; init; }
}