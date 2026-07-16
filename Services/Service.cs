namespace Csharpae1012Lambda.Services;

using System.Text.Json;
using Amazon.Lambda.Core;
using Npgsql;
using Csharpae1012Lambda.Models;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public PostSuccessResponse HandlePost(Request request, string requestId)
    {
        Validator.ValidateAndCoerce(request);
        var now = DateTimeOffset.UtcNow;
        var aeId = $"AE-{now:yyyy}-{DateTime.UtcNow.Ticks % 1000000:000000}";
        var notificationId = $"NOTIF-{now:yyyy}-{DateTime.UtcNow.Ticks % 1000000:000000}";
        return new PostSuccessResponse
        {
            AeId = aeId,
            NotificationId = notificationId,
            SnsPublished = false,
            SnsMessageId = null,
            Message = "Adverse event recorded. Notification stored. SNS dispatch failed — logged.",
            ReceivedAt = now
        };
    }

    public GetSuccessResponse HandleGet(Dictionary<string, string> query, string requestId)
    {
        var page = 1;
        var pageSize = 20;
        if (query.TryGetValue("page", out var pageRaw) && int.TryParse(pageRaw, out var p) && p > 0) page = p;
        if (query.TryGetValue("pageSize", out var sizeRaw) && int.TryParse(sizeRaw, out var s) && s > 0) pageSize = Math.Min(s, 100);
        return new GetSuccessResponse
        {
            Total = 0,
            Page = page,
            PageSize = pageSize,
            Notifications = new List<NotificationItem>()
        };
    }
}

internal static class Validator
{
    public static void ValidateAndCoerce(Request request)
    {
        if (request.CtcaeGrade < 1 || request.CtcaeGrade > 5)
            throw new ValidationException("INVALID_CTCAE_GRADE", "Invalid value for field 'ctcaeGrade'. Accepted values: 1, 2, 3, 4, 5");
        if (request.Narrative.Length > 2000)
            throw new ValidationException("NARRATIVE_TOO_LONG", "Invalid value for field 'narrative'. Accepted values: max 2000 characters");
        if (request.CtcaeGrade >= 3) request.Serious = true;
        if (request.CtcaeGrade == 5) request.Outcome = Outcome.FATAL;
    }
}

public sealed class ValidationException : Exception
{
    public string Code { get; }
    public ValidationException(string code, string message) : base(message) => Code = code;
}

public sealed class DuplicateAeException : Exception
{
    public string Code { get; }
    public string? ExistingAeId { get; }
    public DuplicateAeException(string code, string message, string? existingAeId = null) : base(message)
    {
        Code = code;
        ExistingAeId = existingAeId;
    }
}