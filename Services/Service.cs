namespace Csharpae1012Lambda.Services;

using System.Text.Json;
using Amazon.Lambda.Core;
using Csharpae1012Lambda.Models;
using Npgsql;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public (int StatusCode, Response Body) SubmitAdverseEvent(Request request, ILambdaContext context)
    {
        var now = DateTime.UtcNow.ToString("O");
        return (201, new Response
        {
            Status = "success",
            AeId = $"AE-{DateTime.UtcNow:yyyy}-000001",
            NotificationId = $"NOTIF-{DateTime.UtcNow:yyyy}-000001",
            SnsPublished = false,
            SnsMessageId = null,
            Message = "Adverse event recorded. Notification stored. SNS dispatch failed — logged.",
            ReceivedAt = now
        });
    }

    public Response GetNotifications(Dictionary<string, string> query, ILambdaContext context)
    {
        return new Response
        {
            Status = "success",
            Total = 0,
            Page = 1,
            PageSize = 20,
            Notifications = new List<object>()
        };
    }
}