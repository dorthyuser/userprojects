using System.ComponentModel.DataAnnotations;
using csharpapi248pm.Models;
using csharpapi248pm.Repositories;

namespace csharpapi248pm.Services;

public sealed class AdverseEventService : IAdverseEventService
{
    private readonly IAdverseEventRepository _repository;
    private readonly ILogger<AdverseEventService> _logger;

    public AdverseEventService(IAdverseEventRepository repository, ILogger<AdverseEventService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AdverseEventSubmitResponse> SubmitAsync(AdverseEventRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        ValidateModel(request);
        if (request.CtcaeGrade < 1 || request.CtcaeGrade > 5)
        {
            _logger.LogWarning("Validation failed for field {Field}: {Reason}", nameof(request.CtcaeGrade), "ctcaeGrade must be between 1 and 5");
            throw new ValidationException("INVALID_CTCAE_GRADE");
        }
        if (request.Narrative.Length > 2000)
        {
            _logger.LogWarning("Validation failed for field {Field}: {Reason}", nameof(request.Narrative), "narrative exceeds 2000 characters");
            throw new ValidationException("NARRATIVE_TOO_LONG");
        }
        if (!Enum.IsDefined(typeof(OutcomeEnum), request.Outcome))
        {
            _logger.LogWarning("Validation failed for field {Field}: {Reason}", nameof(request.Outcome), "allowed values are ONGOING, RESOLVED, FATAL, UNKNOWN");
            throw new ValidationException("INVALID_OUTCOME");
        }
        if (!Enum.IsDefined(typeof(ActionTakenEnum), request.ActionTaken))
        {
            _logger.LogWarning("Validation failed for field {Field}: {Reason}", nameof(request.ActionTaken), "allowed values are NONE, DOSE_REDUCED, DRUG_WITHDRAWN, HOSPITALISED");
            throw new ValidationException("INVALID_ACTION_TAKEN");
        }
        _logger.LogInformation("Validation passed.");
        if (request.CtcaeGrade >= 3) request.Serious = true;
        if (request.CtcaeGrade == 5) request.Outcome = OutcomeEnum.FATAL;
        var result = await _repository.SubmitAsync(request, cancellationToken);
        _logger.LogInformation("Response sent with identifier {Identifier}", result.AeId);
        return result;
    }

    public async Task<AdverseEventNotificationsResponse> GetNotificationsAsync(AdverseEventNotificationsQueryRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        ValidateModel(request);
        if (request.DateFrom.HasValue && request.DateTo.HasValue && request.DateFrom > request.DateTo)
        {
            _logger.LogWarning("Validation failed for field {Field}: {Reason}", nameof(request.DateFrom), "dateFrom must be less than or equal to dateTo");
            throw new ValidationException("INVALID_QUERY_PARAM");
        }
        _logger.LogInformation("Validation passed.");
        return await _repository.GetNotificationsAsync(request, cancellationToken);
    }

    private static void ValidateModel(object model)
    {
        var context = new ValidationContext(model);
        Validator.ValidateObject(model, context, true);
    }
}