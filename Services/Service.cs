using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.Logging;
using Npgsql;
using Csharpae1039Lambda.Models;

namespace Csharpae1039Lambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<Service> _logger;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Service>();
    }

    public async Task<(int StatusCode, string Body)> SubmitAdverseEventAsync(string? body, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        if (string.IsNullOrWhiteSpace(body)) return (400, JsonSerializer.Serialize(new ErrorResponse { Code = "MISSING_REQUIRED_FIELD", Message = "Request body is missing." }));
        AdverseEventRequest? request;
        try { request = JsonSerializer.Deserialize<AdverseEventRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { return (400, JsonSerializer.Serialize(new ErrorResponse { Code = "MISSING_REQUIRED_FIELD", Message = "Malformed request body." })); }
        if (request is null) return (400, JsonSerializer.Serialize(new ErrorResponse { Code = "MISSING_REQUIRED_FIELD", Message = "Malformed request body." }));
        var validationError = ValidateRequest(request);
        if (validationError is not null) return validationError.Value;
        var coercedSerious = request.CtcaeGrade >= 3;
        var coercedOutcome = request.CtcaeGrade == 5 ? OutcomeEnum.FATAL : request.Outcome!.Value;
        var priority = request.CtcaeGrade >= 3 ? PriorityEnum.HIGH : PriorityEnum.NORMAL;
        var trialExists = await ExistsAsync("SELECT id FROM trials WHERE trial_id = @trial_id AND status = 'ACTIVE'", "trial_id", request.TrialId!, cancellationToken);
        if (!trialExists) return (400, JsonSerializer.Serialize(new ErrorResponse { Code = "TRIAL_NOT_FOUND", Message = "Trial not found." }));
        var patientExists = await ExistsAsync("SELECT id FROM trial_enrolments WHERE trial_id = @trial_id AND patient_id = @patient_id AND status = 'ENROLLED'", "trial_id", request.TrialId!, "patient_id", request.PatientId!, cancellationToken);
        if (!patientExists) return (400, JsonSerializer.Serialize(new ErrorResponse { Code = "PATIENT_NOT_FOUND", Message = "Patient not found." }));
        var duplicate = await GetDuplicateAsync(request, cancellationToken);
        if (duplicate is not null) return (409, JsonSerializer.Serialize(new ErrorResponse { Code = "DUPLICATE_AE", Message = duplicate }));
        var year = DateTime.UtcNow.Year;
        var aeId = $"AE-{year}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var notificationId = $"NOTIF-{year}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await InsertAeAsync(conn, tx, aeId, request, coercedSerious, coercedOutcome, cancellationToken);
            await InsertNotificationAsync(conn, tx, notificationId, aeId, request, coercedSerious, coercedOutcome, priority, cancellationToken);
            await InsertAuditAsync(conn, tx, aeId, request, coercedSerious, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            var response = new SuccessResponse { Status = "success", AeId = aeId, NotificationId = notificationId, Message = "Adverse event recorded and notification stored.", ReceivedAt = DateTime.UtcNow };
            return (201, JsonSerializer.Serialize(response));
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            return (500, JsonSerializer.Serialize(new ErrorResponse { Code = "DB_ERROR", Message = "Database write failure." }));
        }
    }

    public async Task<(int StatusCode, string Body)> GetNotificationsAsync(IDictionary<string, string>? queryParams, CancellationToken cancellationToken)
    {
        var page = 1;
        var pageSize = 20;
        if (queryParams is not null)
        {
            if (queryParams.TryGetValue("page", out var pageRaw) && int.TryParse(pageRaw, out var parsedPage) && parsedPage > 0) page = parsedPage;
            if (queryParams.TryGetValue("pageSize", out var pageSizeRaw) && int.TryParse(pageSizeRaw, out var parsedPageSize) && parsedPageSize > 0) pageSize = parsedPageSize;
        }
        if (pageSize > 100) return (400, JsonSerializer.Serialize(new ErrorResponse { Code = "INVALID_PAGE_SIZE", Message = "pageSize exceeds maximum." }));
        var response = new NotificationsResponse { Status = "success", Page = page, PageSize = pageSize };
        return (200, JsonSerializer.Serialize(response));
    }

    private static (int StatusCode, string Body)? ValidateRequest(AdverseEventRequest request)
    {
        var ctx = new ValidationContext(request);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, ctx, results, true)) return (400, JsonSerializer.Serialize(new ErrorResponse { Code = "MISSING_REQUIRED_FIELD", Message = "Validation failed." }));
        if (request.CtcaeGrade is null || request.CtcaeGrade < 1 || request.CtcaeGrade > 5) return (400, JsonSerializer.Serialize(new ErrorResponse { Code = "INVALID_CTCAE_GRADE", Message = "Invalid CTCAE grade." }));
        if (request.Outcome is null) return (400, JsonSerializer.Serialize(new ErrorResponse { Code = "INVALID_OUTCOME", Message = "Invalid outcome." }));
        if (request.ActionTaken is null) return (400, JsonSerializer.Serialize(new ErrorResponse { Code = "INVALID_ACTION_TAKEN", Message = "Invalid action taken." }));
        if (request.Narrative is not null && request.Narrative.Length > 2000) return (400, JsonSerializer.Serialize(new ErrorResponse { Code = "NARRATIVE_TOO_LONG", Message = "Narrative too long." }));
        return null;
    }

    private async Task<bool> ExistsAsync(string sql, string key1, string value1, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(sql, _dataSource.CreateConnection());
        cmd.Parameters.AddWithValue(key1, value1);
        await cmd.Connection!.OpenAsync(cancellationToken);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private async Task<bool> ExistsAsync(string sql, string key1, string value1, string key2, string value2, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(key1, value1);
        cmd.Parameters.AddWithValue(key2, value2);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private async Task<string?> GetDuplicateAsync(AdverseEventRequest request, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT ae_id FROM adverse_events WHERE trial_id = @trial_id AND patient_id = @patient_id AND ae_term_code = @ae_term_code AND submitted_at >= NOW() - INTERVAL '60 seconds' LIMIT 1", conn);
        cmd.Parameters.AddWithValue("trial_id", request.TrialId!);
        cmd.Parameters.AddWithValue("patient_id", request.PatientId!);
        cmd.Parameters.AddWithValue("ae_term_code", request.AeTermCode!);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result?.ToString();
    }

    private static async Task InsertAeAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string aeId, AdverseEventRequest request, bool serious, OutcomeEnum outcome, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (@ae_id, @trial_id, @site_id, @patient_id, @clinician_id, @event_date, @ae_term_code, @ae_term_name, @ctcae_grade, @serious, @outcome, @action_taken, @narrative, @related_drug_id, @reported_by, NOW())", conn, tx);
        cmd.Parameters.AddWithValue("ae_id", aeId);
        cmd.Parameters.AddWithValue("trial_id", request.TrialId!);
        cmd.Parameters.AddWithValue("site_id", request.SiteId!);
        cmd.Parameters.AddWithValue("patient_id", request.PatientId!);
        cmd.Parameters.AddWithValue("clinician_id", request.ClinicianId!);
        cmd.Parameters.AddWithValue("event_date", request.EventDate!.Value.ToUniversalTime());
        cmd.Parameters.AddWithValue("ae_term_code", request.AeTermCode!);
        cmd.Parameters.AddWithValue("ae_term_name", request.AeTermName!);
        cmd.Parameters.AddWithValue("ctcae_grade", request.CtcaeGrade!.Value);
        cmd.Parameters.AddWithValue("serious", serious);
        cmd.Parameters.AddWithValue("outcome", outcome);
        cmd.Parameters.AddWithValue("action_taken", request.ActionTaken!.Value);
        cmd.Parameters.AddWithValue("narrative", (object?)request.Narrative ?? DBNull.Value);
        cmd.Parameters.AddWithValue("related_drug_id", (object?)request.RelatedDrugId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("reported_by", request.ReportedBy!);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertNotificationAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string notificationId, string aeId, AdverseEventRequest request, bool serious, OutcomeEnum outcome, PriorityEnum priority, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, created_at) VALUES (@notification_id, @ae_id, @trial_id, @site_id, @patient_id, @ae_term_name, @ctcae_grade, @serious, @outcome, @priority, FALSE, NOW())", conn, tx);
        cmd.Parameters.AddWithValue("notification_id", notificationId);
        cmd.Parameters.AddWithValue("ae_id", aeId);
        cmd.Parameters.AddWithValue("trial_id", request.TrialId!);
        cmd.Parameters.AddWithValue("site_id", request.SiteId!);
        cmd.Parameters.AddWithValue("patient_id", request.PatientId!);
        cmd.Parameters.AddWithValue("ae_term_name", request.AeTermName!);
        cmd.Parameters.AddWithValue("ctcae_grade", request.CtcaeGrade!.Value);
        cmd.Parameters.AddWithValue("serious", serious);
        cmd.Parameters.AddWithValue("outcome", outcome);
        cmd.Parameters.AddWithValue("priority", priority);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertAuditAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string aeId, AdverseEventRequest request, bool serious, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes, performed_at) VALUES (@ae_id, 'CREATED', @performed_by, @sae, @notes, NOW())", conn, tx);
        cmd.Parameters.AddWithValue("ae_id", aeId);
        cmd.Parameters.AddWithValue("performed_by", request.ReportedBy!);
        cmd.Parameters.AddWithValue("sae", serious);
        cmd.Parameters.AddWithValue("notes", $"AE created with grade {request.CtcaeGrade!.Value}.");
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}