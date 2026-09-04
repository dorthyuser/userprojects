using System.Data;
using System.Globalization;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.Logging;
using Npgsql;
using Csharpae1140Lambda.Models;

namespace Csharpae1140Lambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<Service> _logger;

    public Service(NpgsqlDataSource dataSource, ILogger<Service> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<AdverseEventResponse> SubmitAdverseEventAsync(string? body, string requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        if (string.IsNullOrWhiteSpace(body)) throw new ValidationException("INVALID_REQUEST", "Request body is missing.");
        AdverseEventRequest? request;
        try { request = JsonSerializer.Deserialize<AdverseEventRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { throw new ValidationException("INVALID_REQUEST", "Request body is malformed."); }
        if (request is null) throw new ValidationException("INVALID_REQUEST", "Request body is malformed.");
        ValidateRequest(request);
        _logger.LogInformation("Validation passed.");
        var coercedGrade = request.CtcaeGrade!.Value;
        var serious = coercedGrade >= 3 || request.Serious == true;
        var outcome = coercedGrade == 5 ? "FATAL" : request.Outcome!;
        var priority = coercedGrade >= 3 ? "HIGH" : "NORMAL";
        var eventDateUtc = DateTime.SpecifyKind(request.EventDate!.Value, DateTimeKind.Utc);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await EnsureTrialExistsAsync(conn, tx, request.TrialId!, cancellationToken);
            await EnsurePatientEnrolledAsync(conn, tx, request.TrialId!, request.PatientId!, cancellationToken);
            var duplicate = await CheckDuplicateAsync(conn, tx, request.TrialId!, request.PatientId!, request.AeTermCode!, cancellationToken);
            if (duplicate is not null) throw new DuplicateAeException(duplicate);
            var year = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
            var aeSeq = await GetNextSequenceAsync(conn, tx, "ae_id_seq", cancellationToken);
            var notifSeq = await GetNextSequenceAsync(conn, tx, "notif_id_seq", cancellationToken);
            var aeId = $"AE-{year}-{aeSeq:D6}";
            var notificationId = $"NOTIF-{year}-{notifSeq:D6}";
            _logger.LogInformation("DB operation: adverse_events INSERT");
            await InsertAdverseEventAsync(conn, tx, aeId, request, eventDateUtc, serious, outcome, cancellationToken);
            _logger.LogInformation("DB operation: ae_notifications INSERT");
            await InsertNotificationAsync(conn, tx, notificationId, aeId, request, serious, outcome, priority, cancellationToken);
            _logger.LogInformation("DB operation: ae_audit_log INSERT");
            await InsertAuditAsync(conn, tx, aeId, request, serious, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return new AdverseEventResponse { Status = "success", AeId = aeId, NotificationId = notificationId, Message = "Adverse event recorded and notification stored.", ReceivedAt = DateTime.UtcNow };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<NotificationListResponse> GetNotificationsAsync(IDictionary<string, string>? queryParams, string requestId, CancellationToken cancellationToken)
    {
        var page = 1;
        var pageSize = 20;
        if (queryParams is not null)
        {
            if (queryParams.TryGetValue("page", out var pageValue) && int.TryParse(pageValue, out var parsedPage) && parsedPage > 0) page = parsedPage;
            if (queryParams.TryGetValue("pageSize", out var pageSizeValue) && int.TryParse(pageSizeValue, out var parsedPageSize) && parsedPageSize > 0) pageSize = parsedPageSize;
        }
        if (pageSize > 100) throw new ValidationException("INVALID_PAGE_SIZE", "pageSize exceeds maximum of 100.");
        var where = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        AddFilter(queryParams, "trialId", "trial_id", where, parameters);
        AddFilter(queryParams, "siteId", "site_id", where, parameters);
        AddIntFilter(queryParams, "ctcaeGrade", "ctcae_grade", where, parameters);
        AddBoolFilter(queryParams, "serious", "serious", where, parameters);
        AddBoolFilter(queryParams, "acknowledged", "acknowledged", where, parameters);
        AddFilter(queryParams, "priority", "priority", where, parameters);
        AddDateFilter(queryParams, "dateFrom", "created_at >=", where, parameters);
        AddDateFilter(queryParams, "dateTo", "created_at <=", where, parameters);
        var whereSql = where.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", where);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var totalSql = "SELECT COUNT(*) FROM ae_notifications" + whereSql;
        await using var totalCmd = new NpgsqlCommand(totalSql, conn);
        foreach (var p in parameters) totalCmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        var total = Convert.ToInt32(await totalCmd.ExecuteScalarAsync(cancellationToken));
        var selectSql = "SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications" + whereSql + " ORDER BY created_at DESC LIMIT @limit OFFSET @offset";
        await using var cmd = new NpgsqlCommand(selectSql, conn);
        foreach (var p in parameters) cmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        var notifications = new List<NotificationResponse>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            notifications.Add(new NotificationResponse { NotificationId = reader.GetString(0), AeId = reader.GetString(1), TrialId = reader.GetString(2), SiteId = reader.GetString(3), PatientId = reader.GetString(4), AeTermName = reader.GetString(5), CtcaeGrade = reader.GetInt16(6), Serious = reader.GetBoolean(7), Priority = reader.GetString(8), Outcome = reader.GetString(9), Acknowledged = reader.GetBoolean(10), AcknowledgedBy = reader.IsDBNull(11) ? null : reader.GetString(11), AcknowledgedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12), CreatedAt = reader.GetDateTime(13) });
        }
        return new NotificationListResponse { Status = "success", Total = total, Page = page, PageSize = pageSize, Notifications = notifications };
    }

    private static void ValidateRequest(AdverseEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TrialId) || string.IsNullOrWhiteSpace(request.SiteId) || string.IsNullOrWhiteSpace(request.PatientId) || string.IsNullOrWhiteSpace(request.ClinicianId) || request.EventDate is null || string.IsNullOrWhiteSpace(request.AeTermCode) || string.IsNullOrWhiteSpace(request.AeTermName) || request.CtcaeGrade is null || request.Serious is null || string.IsNullOrWhiteSpace(request.Outcome) || string.IsNullOrWhiteSpace(request.ActionTaken) || string.IsNullOrWhiteSpace(request.Narrative) || string.IsNullOrWhiteSpace(request.ReportedBy)) throw new ValidationException("MISSING_REQUIRED_FIELD", "One or more required fields are missing.");
        if (request.CtcaeGrade < 1 || request.CtcaeGrade > 5) throw new ValidationException("INVALID_CTCAE_GRADE", "ctcaeGrade must be between 1 and 5.");
        if (!new[] { "ONGOING", "RESOLVED", "FATAL", "UNKNOWN" }.Contains(request.Outcome)) throw new ValidationException("INVALID_OUTCOME", "outcome is invalid.");
        if (!new[] { "NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED" }.Contains(request.ActionTaken)) throw new ValidationException("INVALID_ACTION_TAKEN", "actionTaken is invalid.");
        if (request.Narrative!.Length > 2000) throw new ValidationException("NARRATIVE_TOO_LONG", "narrative exceeds 2000 characters.");
    }

    private async Task EnsureTrialExistsAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string trialId, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("SELECT id FROM trials WHERE trial_id = @trial_id AND status = 'ACTIVE'", conn, tx);
        cmd.Parameters.AddWithValue("trial_id", trialId);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result is null) throw new ValidationException("TRIAL_NOT_FOUND", "Trial not found.");
    }

    private async Task EnsurePatientEnrolledAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string trialId, string patientId, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("SELECT id FROM trial_enrolments WHERE trial_id = @trial_id AND patient_id = @patient_id AND status = 'ENROLLED'", conn, tx);
        cmd.Parameters.AddWithValue("trial_id", trialId);
        cmd.Parameters.AddWithValue("patient_id", patientId);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result is null) throw new ValidationException("PATIENT_NOT_FOUND", "Patient not enrolled.");
    }

    private async Task<string?> CheckDuplicateAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string trialId, string patientId, string aeTermCode, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("SELECT ae_id FROM adverse_events WHERE trial_id = @trial_id AND patient_id = @patient_id AND ae_term_code = @ae_term_code AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1", conn, tx);
        cmd.Parameters.AddWithValue("trial_id", trialId);
        cmd.Parameters.AddWithValue("patient_id", patientId);
        cmd.Parameters.AddWithValue("ae_term_code", aeTermCode);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    private static async Task<long> GetNextSequenceAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string sequenceName, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand($"SELECT nextval('{sequenceName}')", conn, tx);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task InsertAdverseEventAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string aeId, AdverseEventRequest request, DateTime eventDateUtc, bool serious, string outcome, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (@ae_id, @trial_id, @site_id, @patient_id, @clinician_id, @event_date, @ae_term_code, @ae_term_name, @ctcae_grade, @serious, @outcome, @action_taken, @narrative, @related_drug_id, @reported_by, NOW())", conn, tx);
        cmd.Parameters.AddWithValue("ae_id", aeId);
        cmd.Parameters.AddWithValue("trial_id", request.TrialId!);
        cmd.Parameters.AddWithValue("site_id", request.SiteId!);
        cmd.Parameters.AddWithValue("patient_id", request.PatientId!);
        cmd.Parameters.AddWithValue("clinician_id", request.ClinicianId!);
        cmd.Parameters.AddWithValue("event_date", eventDateUtc);
        cmd.Parameters.AddWithValue("ae_term_code", request.AeTermCode!);
        cmd.Parameters.AddWithValue("ae_term_name", request.AeTermName!);
        cmd.Parameters.AddWithValue("ctcae_grade", request.CtcaeGrade!.Value);
        cmd.Parameters.AddWithValue("serious", serious);
        cmd.Parameters.AddWithValue("outcome", outcome);
        cmd.Parameters.AddWithValue("action_taken", request.ActionTaken!);
        cmd.Parameters.AddWithValue("narrative", request.Narrative!);
        cmd.Parameters.AddWithValue("related_drug_id", (object?)request.RelatedDrugId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("reported_by", request.ReportedBy!);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertNotificationAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string notificationId, string aeId, AdverseEventRequest request, bool serious, string outcome, string priority, CancellationToken cancellationToken)
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
        cmd.Parameters.AddWithValue("notes", $"AE created for trial {request.TrialId} and patient {request.PatientId}.");
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddFilter(IDictionary<string, string>? queryParams, string queryKey, string column, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is null || !queryParams.TryGetValue(queryKey, out var value) || string.IsNullOrWhiteSpace(value)) return;
        where.Add($"{column} = @{queryKey}");
        parameters.Add(new NpgsqlParameter(queryKey, value));
    }

    private static void AddIntFilter(IDictionary<string, string>? queryParams, string queryKey, string column, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is null || !queryParams.TryGetValue(queryKey, out var value) || !int.TryParse(value, out var parsed)) return;
        where.Add($"{column} = @{queryKey}");
        parameters.Add(new NpgsqlParameter(queryKey, parsed));
    }

    private static void AddBoolFilter(IDictionary<string, string>? queryParams, string queryKey, string column, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is null || !queryParams.TryGetValue(queryKey, out var value) || !bool.TryParse(value, out var parsed)) return;
        where.Add($"{column} = @{queryKey}");
        parameters.Add(new NpgsqlParameter(queryKey, parsed));
    }

    private static void AddDateFilter(IDictionary<string, string>? queryParams, string queryKey, string columnExpr, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is null || !queryParams.TryGetValue(queryKey, out var value) || !DateTime.TryParse(value, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)) return;
        where.Add($"{columnExpr} @{queryKey}");
        parameters.Add(new NpgsqlParameter(queryKey, parsed));
    }
}

public sealed class ValidationException : Exception
{
    public ValidationException(string code, string message) : base(message) { Code = code; }
    public string Code { get; }
    public int StatusCode => 400;
}

public sealed class DuplicateAeException : Exception
{
    public DuplicateAeException(string existingAeId) : base("Duplicate adverse event.") { ExistingAeId = existingAeId; }
    public string ExistingAeId { get; }
}