using System;
using System.Collections.Generic;
using System.Globalization;
using azurefunctionaeproject.Models;

namespace azurefunctionaeproject.Helpers;

public static class NotificationQueryParser
{
    public static NotificationQuery Parse(string queryString)
    {
        var query = new NotificationQuery();
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return query;
        }

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
                case "ctcaeGrade": query.CtcaeGrade = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "serious": query.Serious = bool.Parse(value); break;
                case "acknowledged": query.Acknowledged = bool.Parse(value); break;
                case "priority": query.Priority = Enum.Parse<PriorityEnum>(value, true); break;
                case "dateFrom": query.DateFrom = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal); break;
                case "dateTo": query.DateTo = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal); break;
                case "page": query.Page = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "pageSize": query.PageSize = int.Parse(value, CultureInfo.InvariantCulture); break;
                default: break;
            }
        }

        query.Validate();
        return query;
    }
}
