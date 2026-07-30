namespace Csharpae1039Lambda;

using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class Service
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<Service> _logger;

    public Service(NpgsqlDataSource dataSource, ILogger<Service> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<APIGatewayProxyResponse> HandlePostAsync(string? body, string requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        if (string.IsNullOrWhiteSpace(body)) return Error(400, "MISSING_REQUIRED_FIELD", "Request body is missing.");
        AdverseEventRequest? request;
        try { request = JsonSerializer.Deserialize<AdverseEventRequest>(body, JsonOptions); }
        catch { return Error(400, "MISSING_REQUIRED_FIELD", "Malformed JSON."); }
        if (request is null) return Error(400, "MISSING_REQUIRED_FIELD", "Request body is missing.");
        var validationError = ValidateRequest(request);
        if (validationError is not null) return validationError;
        _logger.LogInformation("Validation passed.");
        var grade = request.CtcaeGrade!.Value;
        var serious = grade >= 3 || request.Serious == true;
        var outcome = grade == 5 ? AeOutcome.FATAL : request.Outcome!.Value;
        var priority = grade >= 3 ? NotificationPriority.HIGH : NotificationPriority.NORMAL;
        var eventDateUtc = request.EventDate!.Value.UtcDateTime;
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await EnsureTrialAndPatientAsync(conn, request, cancellationToken).ConfigureAwait(false);
        var duplicate = await CheckDuplicateAsync(conn, request, eventDateUtc, cancellationToken).ConfigureAwait(false);
        if (duplicate is not null) return duplicate;
        var year = DateTime.UtcNow.Year;
        var aeSeq = await GetNextSequenceAsync(conn, "ae_id_seq", cancellationToken).ConfigureAwait(false);
        var notifSeq = await GetNextSequenceAsync(conn, "notif_id_seq", cancellationToken).ConfigureAwait(false);
        var aeId = $"AE-{year}-{aeSeq:000000}";
        var notificationId = $"NOTIF-{year}-{notifSeq:000000}";
        await using var tx = await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("DB_WRITE adverse_events INSERT");
            await using (var cmd = new NpgsqlCommand(@"INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (@ae_id, @trial_id, @site_id, @patient_id, @clinician_id, @event_date, @ae_term_code, @ae_term_name, @ctcae_grade, @serious, @outcome, @action_taken, @narrative, @related_drug_id, @reported_by, NOW()) RETURNING id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("ae_id", aeId);
                cmd.Parameters.AddWithValue("trial_id", request.TrialId!);
                cmd.Parameters.AddWithValue("site_id", request.SiteId!);
                cmd.Parameters.AddWithValue("patient_id", request.PatientId!);
                cmd.Parameters.AddWithValue("clinician_id", request.ClinicianId!);
                cmd.Parameters.AddWithValue("event_date", eventDateUtc);
                cmd.Parameters.AddWithValue("ae_term_code", request.AeTermCode!);
                cmd.Parameters.AddWithValue("ae_term_name", request.AeTermName!);
                cmd.Parameters.AddWithValue("ctcae_grade", grade);
                cmd.Parameters.AddWithValue("serious", serious);
                cmd.Parameters.AddWithValue("outcome", outcome);
                cmd.Parameters.AddWithValue("action_taken", request.ActionTaken!.Value);
                cmd.Parameters.AddWithValue("narrative", request.Narrative!);
                cmd.Parameters.AddWithValue("related_drug_id", (object?)request.RelatedDrugId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("reported_by", request.ReportedBy!);
                await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
            _logger.LogInformation("DB_WRITE ae_notifications INSERT");
            await using (var cmd = new NpgsqlCommand(@"INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at) VALUES (@notification_id, @ae_id, @trial_id, @site_id, @patient_id, @ae_term_name, @ctcae_grade, @serious, @outcome, @priority, FALSE, NULL, NULL);", conn, tx))
            {
                cmd.Parameters.AddWithValue("notification_id", notificationId);
                cmd.Parameters.AddWithValue("ae_id", aeId);
                cmd.Parameters.AddWithValue("trial_id", request.TrialId!);
                cmd.Parameters.AddWithValue("site_id", request.SiteId!);
                cmd.Parameters.AddWithValue("patient_id", request.PatientId!);
                cmd.Parameters.AddWithValue("ae_term_name", request.AeTermName!);
                cmd.Parameters.AddWithValue("ctcae_grade", grade);
                cmd.Parameters.AddWithValue("serious", serious);
                cmd.Parameters.AddWithValue("outcome", outcome);
                cmd.Parameters.AddWithValue("priority", priority);
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            _logger.LogInformation("DB_WRITE ae_audit_log INSERT");
            await using (var cmd = new NpgsqlCommand(@"INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes, performed_at) VALUES (@ae_id, @action, @performed_by, @sae, @notes, NOW());", conn, tx))
            {
                cmd.Parameters.AddWithValue("ae_id", aeId);
                cmd.Parameters.AddWithValue("action", AuditAction.CREATED);
                cmd.Parameters.AddWithValue("performed_by", request.ReportedBy!);
                cmd.Parameters.AddWithValue("sae", serious);
                cmd.Parameters.AddWithValue("notes", $"AE created for trial {request.TrialId}.");
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Success(201, new PostAdverseEventResponse { Status = "success", AeId = aeId, NotificationId = notificationId, Message = "Adverse event recorded and notification stored.", ReceivedAt = DateTimeOffset.UtcNow });
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return Error(500, "DB_ERROR", "Database write failure.");
        }
    }

    public async Task<APIGatewayProxyResponse> HandleGetAsync(Dictionary<string, string>? query, string requestId, CancellationToken cancellationToken)
    {
        var filters = query ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var page = ParseInt(filters, "page", 1);
        var pageSize = ParseInt(filters, "pageSize", 20);
        if (pageSize > 100) return Error(400, "INVALID_PAGE_SIZE", "pageSize exceeds maximum.");
        var where = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        AddFilter(filters, "trialId", "trial_id", where, parameters);
        AddFilter(filters, "siteId", "site_id", where, parameters);
        AddFilter(filters, "ctcaeGrade", "ctcae_grade", where, parameters);
        AddFilter(filters, "serious", "serious", where, parameters);
        AddFilter(filters, "acknowledged", "acknowledged", where, parameters);
        AddFilter(filters, "priority", "priority", where, parameters);
        AddFilter(filters, "dateFrom", "created_at >=", where, parameters, true);
        AddFilter(filters, "dateTo", "created_at <=", where, parameters, true);
        var whereSql = where.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", where);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var total = await CountAsync(conn, whereSql, parameters, cancellationToken).ConfigureAwait(false);
        var items = await SelectAsync(conn, whereSql, parameters, page, pageSize, cancellationToken).ConfigureAwait(false);
        return Success(200, new NotificationListResponse { Status = "success", Total = total, Page = page, PageSize = pageSize, Notifications = items });
    }

    private static APIGatewayProxyResponse Success(int statusCode, object body) => new() { StatusCode = statusCode, Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" }, Body = JsonSerializer.Serialize(body, JsonOptions) };
    private static APIGatewayProxyResponse Error(int statusCode, string code, string message) => Success(statusCode, new { status = "failure", code, message });
    private static int ParseInt(Dictionary<string, string> q, string key, int fallback) => q.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : fallback;
    private static void AddFilter(Dictionary<string, string> q, string key, string column, List<string> where, List<NpgsqlParameter> parameters, bool date = false) { if (!q.TryGetValue(key, out var v) || string.IsNullOrWhiteSpace(v)) return; var p = $"p{parameters.Count}"; where.Add(date ? $"{column} @{p}" : $"{column} = @{p}"); parameters.Add(new NpgsqlParameter(p, v)); }
    private static async Task<long> CountAsync(NpgsqlConnection conn, string whereSql, List<NpgsqlParameter> parameters, CancellationToken ct) { await using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM ae_notifications{whereSql};", conn); cmd.Parameters.AddRange(parameters.ToArray()); return (long)(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) ?? 0L); }
    private static async Task<List<NotificationResponseItem>> SelectAsync(NpgsqlConnection conn, string whereSql, List<NpgsqlParameter> parameters, int page, int pageSize, CancellationToken ct) { await using var cmd = new NpgsqlCommand($"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications{whereSql} ORDER BY created_at DESC LIMIT @limit OFFSET @offset;", conn); cmd.Parameters.AddRange(parameters.ToArray()); cmd.Parameters.AddWithValue("limit", pageSize); cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize); var list = new List<NotificationResponseItem>(); await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); while (await reader.ReadAsync(ct).ConfigureAwait(false)) list.Add(new NotificationResponseItem { NotificationId = reader.GetString(0), AeId = reader.GetString(1), TrialId = reader.GetString(2), SiteId = reader.GetString(3), PatientId = reader.GetString(4), AeTermName = reader.GetString(5), CtcaeGrade = reader.GetInt16(6), Serious = reader.GetBoolean(7), Priority = reader.GetString(8), Outcome = reader.GetString(9), Acknowledged = reader.GetBoolean(10), AcknowledgedBy = reader.IsDBNull(11) ? null : reader.GetString(11), AcknowledgedAt = reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTimeOffset>(12), CreatedAt = reader.GetFieldValue<DateTimeOffset>(13) }); return list; }
    private static async Task EnsureTrialAndPatientAsync(NpgsqlConnection conn, AdverseEventRequest request, CancellationToken ct) { await using var trial = new NpgsqlCommand("SELECT id FROM trials WHERE trial_id = @trial_id AND status = 'ACTIVE';", conn); trial.Parameters.AddWithValue("trial_id", request.TrialId!); if (await trial.ExecuteScalarAsync(ct).ConfigureAwait(false) is null) throw new InvalidOperationException("TRIAL_NOT_FOUND"); await using var patient = new NpgsqlCommand("SELECT id FROM trial_enrolments WHERE trial_id = @trial_id AND patient_id = @patient_id AND status = 'ENROLLED';", conn); patient.Parameters.AddWithValue("trial_id", request.TrialId!); patient.Parameters.AddWithValue("patient_id", request.PatientId!); if (await patient.ExecuteScalarAsync(ct).ConfigureAwait(false) is null) throw new InvalidOperationException("PATIENT_NOT_FOUND"); }
    private static async Task<APIGatewayProxyResponse?> CheckDuplicateAsync(NpgsqlConnection conn, AdverseEventRequest request, DateTime eventDateUtc, CancellationToken ct) { await using var cmd = new NpgsqlCommand("SELECT ae_id FROM adverse_events WHERE trial_id = @trial_id AND patient_id = @patient_id AND ae_term_code = @ae_term_code AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1;", conn); cmd.Parameters.AddWithValue("trial_id", request.TrialId!); cmd.Parameters.AddWithValue("patient_id", request.PatientId!); cmd.Parameters.AddWithValue("ae_term_code", request.AeTermCode!); var existing = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) as string; return existing is null ? null : Success(409, new { status = "failure", code = "DUPLICATE_AE", aeId = existing, message = "Identical AE submitted within 60 seconds." }); }
    private static async Task<long> GetNextSequenceAsync(NpgsqlConnection conn, string sequenceName, CancellationToken ct) { await using var cmd = new NpgsqlCommand($"SELECT nextval('{sequenceName}');", conn); return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false)); }
    private static APIGatewayProxyResponse? ValidateRequest(AdverseEventRequest request)
    {
        var ctx = new ValidationContext(request);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, ctx, results, true)) return Error(400, "MISSING_REQUIRED_FIELD", "Validation failed.");
        if (request.CtcaeGrade is null || request.CtcaeGrade < 1 || request.CtcaeGrade > 5) return Error(400, "INVALID_CTCAE_GRADE", "Invalid CTCAE grade.");
        if (request.Outcome is null) return Error(400, "INVALID_OUTCOME", "Invalid outcome.");
        if (request.ActionTaken is null) return Error(400, "INVALID_ACTION_TAKEN", "Invalid action taken.");
        if (request.Narrative is not null && request.Narrative.Length > 2000) return Error(400, "NARRATIVE_TOO_LONG", "Narrative too long.");
        return null;
    }
}