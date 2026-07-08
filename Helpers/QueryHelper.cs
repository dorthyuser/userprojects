using System;
using AdverseEventReporter.Models;

namespace AdverseEventReporter.Helpers;

public static class QueryHelper
{
    public static NotificationQuery ParseNotificationQuery(string queryString)
    {
        var query = new NotificationQuery();
        if (string.IsNullOrWhiteSpace(queryString)) return query;
        var parts = queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            var key = Uri.UnescapeDataString(kv[0]);
            var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;
            switch (key)
            {
                case "trialId": query.TrialId = value; break;
                case "siteId": query.SiteId = value; break;
                case "ctcaeGrade": if (int.TryParse(value, out var g)) query.CtcaeGrade = g; else throw new ApiException(System.Net.HttpStatusCode.BadRequest, "INVALID_QUERY_PARAM", "Invalid ctcaeGrade."); break;
                case "serious": if (bool.TryParse(value, out var s)) query.Serious = s; else throw new ApiException(System.Net.HttpStatusCode.BadRequest, "INVALID_QUERY_PARAM", "Invalid serious."); break;
                case "acknowledged": if (bool.TryParse(value, out var a)) query.Acknowledged = a; else throw new ApiException(System.Net.HttpStatusCode.BadRequest, "INVALID_QUERY_PARAM", "Invalid acknowledged."); break;
                case "priority": query.Priority = value; break;
                case "dateFrom": if (DateTimeOffset.TryParse(value, out var df)) query.DateFrom = df.UtcDateTime; else throw new ApiException(System.Net.HttpStatusCode.BadRequest, "INVALID_QUERY_PARAM", "Invalid dateFrom."); break;
                case "dateTo": if (DateTimeOffset.TryParse(value, out var dt)) query.DateTo = dt.UtcDateTime; else throw new ApiException(System.Net.HttpStatusCode.BadRequest, "INVALID_QUERY_PARAM", "Invalid dateTo."); break;
                case "page": if (int.TryParse(value, out var p)) query.Page = p; else throw new ApiException(System.Net.HttpStatusCode.BadRequest, "INVALID_QUERY_PARAM", "Invalid page."); break;
                case "pageSize": if (int.TryParse(value, out var ps)) query.PageSize = ps; else throw new ApiException(System.Net.HttpStatusCode.BadRequest, "INVALID_QUERY_PARAM", "Invalid pageSize."); break;
            }
        }
        return query;
    }
}
