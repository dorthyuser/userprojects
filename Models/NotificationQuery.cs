using System;

namespace azurefunctionaeproject.Models;

public sealed class NotificationQuery
{
    public string? TrialId { get; set; }
    public string? SiteId { get; set; }
    public int? CtcaeGrade { get; set; }
    public bool? Serious { get; set; }
    public bool? Acknowledged { get; set; }
    public PriorityEnum? Priority { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public void Validate()
    {
        if (CtcaeGrade.HasValue && (CtcaeGrade < 1 || CtcaeGrade > 5)) throw new Helpers.ValidationException("INVALID_QUERY_PARAM", "ctcaeGrade must be between 1 and 5.");
        if (Page < 1) throw new Helpers.ValidationException("INVALID_QUERY_PARAM", "page must be at least 1.");
        if (PageSize < 1 || PageSize > 100) throw new Helpers.ValidationException("INVALID_QUERY_PARAM", "pageSize must be between 1 and 100.");
    }
}
