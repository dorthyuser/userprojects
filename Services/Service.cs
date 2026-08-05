using System.Text.Json;
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

    public async Task<SubmitAdverseEventResponse> SubmitAdverseEventAsync(string? body, string requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        if (string.IsNullOrWhiteSpace(body)) throw new AdverseEventValidationException(400, "MISSING_REQUIRED_FIELD", "Request body is missing.");
        var request = JsonSerializer.Deserialize<AdverseEventRequest>(body) ?? throw new AdverseEventValidationException(400, "MISSING_REQUIRED_FIELD", "Request body is malformed.");
        ValidateRequest(request);
        _logger.LogInformation("Validation passed.");
        var grade = request.CtcaeGrade!.Value;
        var serious = grade >= 3 || request.Serious == true;
        var outcome = grade == 5 ? "FATAL" : request.Outcome!;
        var priority = grade >= 3 ? "HIGH" : "NORMAL";
        var aeId = $"AE-{DateTime.UtcNow:yyyy}-{GetShortToken()}";
        var notificationId = $"NOTIF-{DateTime.UtcNow:yyyy}-{GetShortToken()}";
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await InsertAeAsync(conn, tx, request, aeId, serious, outcome, cancellationToken);
            await InsertNotificationAsync(conn, tx, request, aeId, notificationId, serious, outcome, priority, cancellationToken);
            await InsertAuditAsync(conn, tx, request, aeId, serious, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return new SubmitAdverseEventResponse { Status = "success", AeId = aeId, NotificationId = notificationId, Message = "Adverse event recorded and notification stored.", ReceivedAt = DateTime.UtcNow };
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
        if (queryParams is not null && queryParams.TryGetValue("pageSize", out var ps) && int.TryParse(ps, out var parsedPs) && parsedPs > 100) throw new AdverseEventValidationException(400, "INVALID_PAGE_SIZE", "pageSize exceeds maximum.");
        var where = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        AddFilter(queryParams, "trialId", "trial_id", where, parameters);
        AddFilter(queryParams, "siteId", "site_id", where, parameters);
        AddFilter(queryParams, "ctcaeGrade", "ctcae_grade", where, parameters);
        AddFilter(queryParams, "serious", "serious", where, parameters);
        AddFilter(queryParams, "acknowledged", "acknowledged", where, parameters);
        AddFilter(queryParams, "priority", "priority", where, parameters);
        AddDateFilter(queryParams, "dateFrom", ">=", where, parameters);
        AddDateFilter(queryParams, "dateTo", "<=", where, parameters);
        var whereSql = where.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", where);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var total = await CountAsync(conn, whereSql, parameters, cancellationToken);
        var notifications = await SelectAsync(conn, whereSql, parameters, page, pageSize, cancellationToken);
        return new NotificationListResponse { Status = "success", Total = total, Page = page, PageSize = pageSize, Notifications = notifications };
    }

    private static void ValidateRequest(AdverseEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TrialId) || string.IsNullOrWhiteSpace(request.SiteId) || string.IsNullOrWhiteSpace(request.PatientId) || string.IsNullOrWhiteSpace(request.ClinicianId) || request.EventDate is null || string.IsNullOrWhiteSpace(request.AeTermCode) || string.IsNullOrWhiteSpace(request.AeTermName) || request.CtcaeGrade is null || request.Serious is null || string.IsNullOrWhiteSpace(request.Outcome) || string.IsNullOrWhiteSpace(request.ActionTaken) || string.IsNullOrWhiteSpace(request.Narrative) || string.IsNullOrWhiteSpace(request.ReportedBy)) throw new AdverseEventValidationException(400, "MISSING_REQUIRED_FIELD", "One or more required fields are missing.");
        if (request.CtcaeGrade < 1 || request.CtcaeGrade > 5) throw new AdverseEventValidationException(400, "INVALID_CTCAE_GRADE", "ctcaeGrade must be between 1 and 5.");
        if (request.Narrative!.Length > 2000) throw new AdverseEventValidationException(400, "NARRATIVE_TOO_LONG", "narrative exceeds 2,000 characters.");
        var outcomes = new[] { "ONGOING", "RESOLVED", "FATAL", "UNKNOWN" };
        if (!outcomes.Contains(request.Outcome)) throw new AdverseEventValidationException(400, "INVALID_OUTCOME", "outcome value is invalid.");
        var actions = new[] { "NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED" };
        if (!actions.Contains(request.ActionTaken)) throw new AdverseEventValidationException(400, "INVALID_ACTION_TAKEN", "actionTaken value is invalid.");
    }

    private async Task InsertAeAsync(NpgsqlConnection conn, NpgsqlTransaction tx, AdverseEventRequest request, string aeId, bool serious, string outcome, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (@ae_id, @trial_id, @site_id, @patient_id, @clinician_id, @event_date, @ae_term_code, @ae_term_name, @ctcae_grade, @serious, @outcome, @action_taken, @narrative, @related_drug_id, @reported_by, NOW())";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
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
        cmd.Parameters.AddWithValue("action_taken", request.ActionTaken!);
        cmd.Parameters.AddWithValue("narrative", request.Narrative!);
        cmd.Parameters.AddWithValue("related_drug_id", (object?)request.RelatedDrugId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("reported_by", request.ReportedBy!);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task InsertNotificationAsync(NpgsqlConnection conn, NpgsqlTransaction tx, AdverseEventRequest request, string aeId, string notificationId, bool serious, string outcome, string priority, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, created_at) VALUES (@notification_id, @ae_id, @trial_id, @site_id, @patient_id, @ae_term_name, @ctcae_grade, @serious, @outcome, @priority, FALSE, NOW())";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
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

    private async Task InsertAuditAsync(NpgsqlConnection conn, NpgsqlTransaction tx, AdverseEventRequest request, string aeId, bool serious, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes, performed_at) VALUES (@ae_id, @action, @performed_by, @sae, @notes, NOW())";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("ae_id", aeId);
        cmd.Parameters.AddWithValue("action", "CREATED");
        cmd.Parameters.AddWithValue("performed_by", request.ReportedBy!);
        cmd.Parameters.AddWithValue("sae", serious);
        cmd.Parameters.AddWithValue("notes", $"AE created for trial {request.TrialId}.");
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddFilter(IDictionary<string, string>? queryParams, string queryKey, string column, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is null || !queryParams.TryGetValue(queryKey, out var value) || string.IsNullOrWhiteSpace(value)) return;
        where.Add($"{column} = @{queryKey}");
        parameters.Add(new NpgsqlParameter(queryKey, value));
    }

    private static void AddDateFilter(IDictionary<string, string>? queryParams, string queryKey, string op, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is null || !queryParams.TryGetValue(queryKey, out var value) || string.IsNullOrWhiteSpace(value)) return;
        where.Add($"created_at {op} @{queryKey}");
        parameters.Add(new NpgsqlParameter(queryKey, DateTime.Parse(value).ToUniversalTime()));
    }

    private static async Task<int> CountAsync(NpgsqlConnection conn, string whereSql, List<NpgsqlParameter> parameters, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM ae_notifications{whereSql}", conn);
        foreach (var p in parameters) cmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task<List<NotificationResponse>> SelectAsync(NpgsqlConnection conn, string whereSql, List<NpgsqlParameter> parameters, int page, int pageSize, CancellationToken cancellationToken)
    {
        var list = new List<NotificationResponse>();
        await using var cmd = new NpgsqlCommand($"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications{whereSql} ORDER BY created_at DESC LIMIT @limit OFFSET @offset", conn);
        foreach (var p in parameters) cmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new NotificationResponse { NotificationId = reader.GetString(0), AeId = reader.GetString(1), TrialId = reader.GetString(2), SiteId = reader.GetString(3), PatientId = reader.GetString(4), AeTermName = reader.GetString(5), CtcaeGrade = reader.GetInt16(6), Serious = reader.GetBoolean(7), Priority = reader.GetString(8), Outcome = reader.GetString(9), Acknowledged = reader.GetBoolean(10), AcknowledgedBy = reader.IsDBNull(11) ? null : reader.GetString(11), AcknowledgedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12), CreatedAt = reader.GetDateTime(13) });
        }
        return list;
    }

    private static string GetShortToken() => Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
}

public sealed class AdverseEventValidationException : Exception
{
    public AdverseEventValidationException(int statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public int StatusCode { get; }
    public string Code { get; }
}

public sealed class AdverseEventDuplicateException : Exception
{
    public AdverseEventDuplicateException(string aeId) : base("Duplicate adverse event.") => AeId = aeId;
    public string AeId { get; }
}