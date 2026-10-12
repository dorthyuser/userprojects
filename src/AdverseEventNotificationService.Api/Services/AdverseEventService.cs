using AdverseEventNotificationService.Api.Contracts;
using AdverseEventNotificationService.Api.Domain;
using AdverseEventNotificationService.Api.Exceptions;
using Microsoft.Extensions.Options;

namespace AdverseEventNotificationService.Api.Services;

public sealed class AdverseEventService : IAdverseEventService
{
    private readonly IAdverseEventRepository _adverseEventRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IIdGenerator _idGenerator;
    private readonly IOptions<DatabaseOptions> _databaseOptions;
    private readonly ILogger<AdverseEventService> _logger;

    public AdverseEventService(IAdverseEventRepository adverseEventRepository, INotificationRepository notificationRepository, IIdGenerator idGenerator, IOptions<DatabaseOptions> databaseOptions, ILogger<AdverseEventService> logger)
    {
        _adverseEventRepository = adverseEventRepository;
        _notificationRepository = notificationRepository;
        _idGenerator = idGenerator;
        _databaseOptions = databaseOptions;
        _logger = logger;
    }

    public async Task<AdverseEventResponseDto> SubmitAsync(AdverseEventRequestDto request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Service entry: submit adverse event");
        var coerced = Coerce(request);
        ValidateBusinessRules(coerced);

        if (!await _adverseEventRepository.TrialExistsAsync(coerced.TrialId, cancellationToken)) throw new NotFoundException("TRIAL_NOT_FOUND");
        if (!await _adverseEventRepository.PatientEnrolledAsync(coerced.TrialId, coerced.PatientId, cancellationToken)) throw new NotFoundException("PATIENT_NOT_FOUND");

        var duplicate = await _adverseEventRepository.FindDuplicateAeIdAsync(coerced.TrialId, coerced.PatientId, coerced.AeTermCode, coerced.CtcaeGrade, DateTimeOffset.UtcNow.AddSeconds(-60), cancellationToken);
        if (duplicate is not null) throw new ConflictException("DUPLICATE_AE");

        var aeId = await _idGenerator.GenerateAeIdAsync(cancellationToken);
        var notificationId = await _idGenerator.GenerateNotificationIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var priority = coerced.CtcaeGrade >= 3 ? AdverseEventPriority.High : AdverseEventPriority.Normal;

        var entity = new AdverseEventEntity(0, aeId, coerced.TrialId, coerced.SiteId, coerced.PatientId, coerced.ClinicianId, coerced.EventDate, coerced.AeTermCode, coerced.AeTermName, coerced.CtcaeGrade, coerced.Serious, coerced.Outcome, coerced.ActionTaken, coerced.Narrative, coerced.RelatedDrugId, coerced.ReportedBy, now, now, now);
        await _adverseEventRepository.InsertAsync(entity, cancellationToken);

        var notification = new AdverseEventNotificationEntity(0, notificationId, aeId, coerced.TrialId, coerced.SiteId, coerced.PatientId, coerced.AeTermName, coerced.CtcaeGrade, coerced.Serious, coerced.Outcome, priority, false, null, null, false, null, now, now);
        await _notificationRepository.InsertAsync(notification, cancellationToken);

        return new AdverseEventResponseDto("success", aeId, notificationId, false, null, now);
    }

    public async Task<AdverseEventNotificationResponseDto> GetNotificationsAsync(NotificationQueryDto query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Service entry: get notifications");
        var page = query.Page.GetValueOrDefault(1);
        var pageSize = Math.Min(query.PageSize.GetValueOrDefault(20), _databaseOptions.Value.MaxPageSize);
        var (total, items) = await _notificationRepository.SearchAsync(query, page, pageSize, cancellationToken);
        return new AdverseEventNotificationResponseDto("success", total, page, pageSize, items);
    }

    private static AdverseEventRequestDto Coerce(AdverseEventRequestDto request)
    {
        var serious = request.CtcaeGrade >= 3 || request.Serious;
        var outcome = request.CtcaeGrade == 5 ? AdverseEventOutcome.Fatal : request.Outcome;
        return request with { Serious = serious, Outcome = outcome };
    }

    private static void ValidateBusinessRules(AdverseEventRequestDto request)
    {
        if (request.CtcaeGrade is < 1 or > 5) throw new BadRequestException("INVALID_CTCAE_GRADE");
        if (request.Narrative.Length > 2000) throw new BadRequestException("NARRATIVE_TOO_LONG");
    }
}