using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.Logging;
using Npgsql;
using Csharpae1140Lambda.Models;
namespace Csharpae1140Lambda.Services;
public sealed class Service
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<Service> _logger;
    public Service(NpgsqlDataSource dataSource, ILogger<Service> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }
    public async Task<AdverseEventSuccessResponse> SubmitAdverseEventAsync(string? body, string requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        if (string.IsNullOrWhiteSpace(body)) throw new AdverseEventValidationException(400, "MISSING_REQUIRED_FIELD", "Request body is missing.");
        AdverseEventRequest? request;
        try { request = JsonSerializer.Deserialize<AdverseEventRequest>(body, JsonOptions); }
        catch { throw new AdverseEventValidationException(400, "MISSING_REQUIRED_FIELD", "Request body is malformed."); }
        if (request is null) throw new AdverseEventValidationException(400, "MISSING_REQUIRED_FIELD", "Request body is malformed.");
        ValidateRequest(request);
        var ctcaeGrade = request.CtcaeGrade!.Value;
        var serious = request.Serious!.Value || ctcaeGrade >= 3;
        var outcome = ctcaeGrade == 5 ? "FATAL" : request.Outcome!;
        var priority = ctcaeGrade >= 3 ? "HIGH" : "NORMAL";
        _logger.LogInformation("Validation passed.");
        var receivedAt = DateTime.UtcNow;
        var year = receivedAt.Year;
        var aeSeq = await GetNextSequenceAsync("ae_id_seq", cancellationToken);
        var notifSeq = await GetNextSequenceAsync("notif_id_seq", cancellationToken);
        var aeId = $"AE-{year}-{aeSeq:D6}";
        var notificationId = $"NOTIF-{year}-{notifSeq:D6}";
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            var duplicateAeId = await CheckDuplicateAsync(conn, request, cancellationToken);
            if (duplicateAeId is not null) throw new AdverseEventConflictException(duplicateAeId);
            await InsertAeAsync(conn, tx, aeId, request, ctcaeGrade, serious, outcome, receivedAt, cancellationToken);
            await InsertNotificationAsync(conn, tx, notificationId, aeId, request, ctcaeGrade, serious, outcome, priority, receivedAt, cancellationToken);
            await InsertAuditAsync(conn, tx, aeId, request, serious, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return new AdverseEventSuccessResponse { Status = "success", AeId = aeId, NotificationId = notificationId, Message = "Adverse event recorded and notification stored.", ReceivedAt = receivedAt };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
    public async Task<NotificationsResponse> GetNotificationsAsync(IDictionary<string, string>? queryParams, string requestId, CancellationToken cancellationToken)
    {
        var page = 1;
        var pageSize = 20;
        if (queryParams is not null)
        {
            if (queryParams.TryGetValue("page", out var pageValue) && int.TryParse(pageValue, out var parsedPage) && parsedPage > 0) page = parsedPage;
            if (queryParams.TryGetValue("pageSize", out var pageSizeValue) && int.TryParse(pageSizeValue, out var parsedPageSize) && parsedPageSize > 0) pageSize = parsedPageSize;
        }
        if (pageSize > 100) throw new AdverseEventValidationException(400, "INVALID_PAGE_SIZE", "pageSize exceeds maximum allowed value.");
        var where = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        AddFilter(queryParams, "trialId", "trial_id", where, parameters);
        AddFilter(queryParams, "siteId", "site_id", where, parameters);
        AddIntFilter(queryParams, "ctcaeGrade", "ctcae_grade", where, parameters);
        AddBoolFilter(queryParams, "serious", "serious", where, parameters);
        AddBoolFilter(queryParams, "acknowledged", "acknowledged", where, parameters);
        AddFilter(queryParams, "priority", "priority", where, parameters);
        AddDateFilter(queryParams, "dateFrom", "created_at >= ", where, parameters);
        AddDateFilter(queryParams, "dateTo", "created_at <= ", where, parameters);
        var whereClause = where.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", where);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var totalSql = $"SELECT COUNT(*) FROM ae_notifications{whereClause}";
        await using var totalCmd = new NpgsqlCommand(totalSql, conn);
        foreach (var p in parameters) totalCmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        var total = Convert.ToInt32(await totalCmd.ExecuteScalarAsync(cancellationToken));
        var selectSql = $"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications{whereClause} ORDER BY created_at DESC LIMIT @limit OFFSET @offset";
        await using var cmd = new NpgsqlCommand(selectSql, conn);
        foreach (var p in parameters) cmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        var items = new List<NotificationItemResponse>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new NotificationItemResponse { NotificationId = reader.GetString(0), AeId = reader.GetString(1), TrialId = reader.GetString(2), SiteId = reader.GetString(3), PatientId = reader.GetString(4), AeTermName = reader.GetString(5), CtcaeGrade = reader.GetInt16(6), Serious = reader.GetBoolean(7), Priority = reader.GetString(8), Outcome = reader.GetString(9), Acknowledged = reader.GetBoolean(10), AcknowledgedBy = reader.IsDBNull(11) ? null : reader.GetString(11), AcknowledgedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12), CreatedAt = reader.GetDateTime(13) });
        }
        return new NotificationsResponse { Status = "success", Total = total, Page = page, PageSize = pageSize, Notifications = items };
    }
    private static void ValidateRequest(AdverseEventRequest request)
    {
        var context = new ValidationContext(request);
        Validator.ValidateObject(request, context, validateAllProperties: true);
        if (request.CtcaeGrade is null || request.CtcaeGrade < 1 || request.CtcaeGrade > 5) throw new AdverseEventValidationException(400, "INVALID_CTCAE_GRADE", "ctcaeGrade must be between 1 and 5.");
        if (string.IsNullOrWhiteSpace(request.Outcome) || !new[] { "ONGOING", "RESOLVED", "FATAL", "UNKNOWN" }.Contains(request.Outcome)) throw new AdverseEventValidationException(400, "INVALID_OUTCOME", "outcome is invalid.");
        if (string.IsNullOrWhiteSpace(request.ActionTaken) || !new[] { "NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED" }.Contains(request.ActionTaken)) throw new AdverseEventValidationException(400, "INVALID_ACTION_TAKEN", "actionTaken is invalid.");
        if (request.Narrative is null || request.Narrative.Length > 2000) throw new AdverseEventValidationException(400, "NARRATIVE_TOO_LONG", "narrative exceeds maximum length.");
        if (string.IsNullOrWhiteSpace(request.TrialId) || string.IsNullOrWhiteSpace(request.SiteId) || string.IsNullOrWhiteSpace(request.PatientId) || string.IsNullOrWhiteSpace(request.ClinicianId) || request.EventDate is null || string.IsNullOrWhiteSpace(request.AeTermCode) || string.IsNullOrWhiteSpace(request.AeTermName) || request.Serious is null || string.IsNullOrWhiteSpace(request.ReportedBy)) throw new AdverseEventValidationException(400, "MISSING_REQUIRED_FIELD", "One or more required fields are missing.");
    }
    private async Task<long> GetNextSequenceAsync(string sequenceName, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand($"SELECT nextval('{sequenceName}')", conn);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
    }
    private async Task<string?> CheckDuplicateAsync(NpgsqlConnection conn, AdverseEventRequest request, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("SELECT ae_id FROM adverse_events WHERE trial_id = @trial_id AND patient_id = @patient_id AND ae_term_code = @ae_term_code AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1", conn);
        cmd.Parameters.AddWithValue("trial_id", request.TrialId!);
        cmd.Parameters.AddWithValue("patient_id", request.PatientId!);
        cmd.Parameters.AddWithValue("ae_term_code", request.AeTermCode!);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }
    private async Task InsertAeAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string aeId, AdverseEventRequest request, int ctcaeGrade, bool serious, string outcome, DateTime receivedAt, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (@ae_id, @trial_id, @site_id, @patient_id, @clinician_id, @event_date, @ae_term_code, @ae_term_name, @ctcae_grade, @serious, @outcome, @action_taken, @narrative, @related_drug_id, @reported_by, @submitted_at, NOW(), NOW())", conn, tx);
        cmd.Parameters.AddWithValue("ae_id", aeId);
        cmd.Parameters.AddWithValue("trial_id", request.TrialId!);
        cmd.Parameters.AddWithValue("site_id", request.SiteId!);
        cmd.Parameters.AddWithValue("patient_id", request.PatientId!);
        cmd.Parameters.AddWithValue("clinician_id", request.ClinicianId!);
        cmd.Parameters.AddWithValue("event_date", DateTime.SpecifyKind(request.EventDate!.Value, DateTimeKind.Utc));
        cmd.Parameters.AddWithValue("ae_term_code", request.AeTermCode!);
        cmd.Parameters.AddWithValue("ae_term_name", request.AeTermName!);
        cmd.Parameters.AddWithValue("ctcae_grade", ctcaeGrade);
        cmd.Parameters.AddWithValue("serious", serious);
        cmd.Parameters.AddWithValue("outcome", outcome);
        cmd.Parameters.AddWithValue("action_taken", request.ActionTaken!);
        cmd.Parameters.AddWithValue("narrative", (object?)request.Narrative ?? DBNull.Value);
        cmd.Parameters.AddWithValue("related_drug_id", (object?)request.RelatedDrugId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("reported_by", request.ReportedBy!);
        cmd.Parameters.AddWithValue("submitted_at", receivedAt);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
    private async Task InsertNotificationAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string notificationId, string aeId, AdverseEventRequest request, int ctcaeGrade, bool serious, string outcome, string priority, DateTime receivedAt, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, created_at, updated_at) VALUES (@notification_id, @ae_id, @trial_id, @site_id, @patient_id, @ae_term_name, @ctcae_grade, @serious, @outcome, @priority, FALSE, @created_at, NOW())", conn, tx);
        cmd.Parameters.AddWithValue("notification_id", notificationId);
        cmd.Parameters.AddWithValue("ae_id", aeId);
        cmd.Parameters.AddWithValue("trial_id", request.TrialId!);
        cmd.Parameters.AddWithValue("site_id", request.SiteId!);
        cmd.Parameters.AddWithValue("patient_id", request.PatientId!);
        cmd.Parameters.AddWithValue("ae_term_name", request.AeTermName!);
        cmd.Parameters.AddWithValue("ctcae_grade", ctcaeGrade);
        cmd.Parameters.AddWithValue("serious", serious);
        cmd.Parameters.AddWithValue("outcome", outcome);
        cmd.Parameters.AddWithValue("priority", priority);
        cmd.Parameters.AddWithValue("created_at", receivedAt);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
    private async Task InsertAuditAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string aeId, AdverseEventRequest request, bool serious, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes, performed_at) VALUES (@ae_id, 'CREATED', @performed_by, @sae, @notes, NOW())", conn, tx);
        cmd.Parameters.AddWithValue("ae_id", aeId);
        cmd.Parameters.AddWithValue("performed_by", request.ReportedBy!);
        cmd.Parameters.AddWithValue("sae", serious);
        cmd.Parameters.AddWithValue("notes", $"AE created for trial {request.TrialId}.");
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
    private static void AddFilter(IDictionary<string, string>? queryParams, string queryKey, string columnName, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is not null && queryParams.TryGetValue(queryKey, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            where.Add($"{columnName} = @{queryKey}");
            parameters.Add(new NpgsqlParameter(queryKey, value));
        }
    }
    private static void AddIntFilter(IDictionary<string, string>? queryParams, string queryKey, string columnName, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is not null && queryParams.TryGetValue(queryKey, out var value) && int.TryParse(value, out var parsed))
        {
            where.Add($"{columnName} = @{queryKey}");
            parameters.Add(new NpgsqlParameter(queryKey, parsed));
        }
    }
    private static void AddBoolFilter(IDictionary<string, string>? queryParams, string queryKey, string columnName, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is not null && queryParams.TryGetValue(queryKey, out var value) && bool.TryParse(value, out var parsed))
        {
            where.Add($"{columnName} = @{queryKey}");
            parameters.Add(new NpgsqlParameter(queryKey, parsed));
        }
    }
    private static void AddDateFilter(IDictionary<string, string>? queryParams, string queryKey, string clause, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is not null && queryParams.TryGetValue(queryKey, out var value) && DateTime.TryParse(value, out var parsed))
        {
            where.Add($"{clause}@{queryKey}");
            parameters.Add(new NpgsqlParameter(queryKey, DateTime.SpecifyKind(parsed, DateTimeKind.Utc)));
        }
    }
}
public sealed class AdverseEventValidationException : Exception
{
    public AdverseEventValidationException(int statusCode, string code, string message) : base(message) { StatusCode = statusCode; Code = code; }
    public int StatusCode { get; }
    public string Code { get; }
}
public sealed class AdverseEventConflictException : Exception
{
    public AdverseEventConflictException(string aeId) : base("Duplicate adverse event.") { AeId = aeId; }
    public string AeId { get; }
}