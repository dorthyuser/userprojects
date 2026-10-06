using System;
using Microsoft.AspNetCore.Http;

namespace BusTravelAccountsEaMainFunction;

public static class CorrelationIdHelper
{
    public static string GetCorrelationId(IHeaderDictionary headers)
    {
        var value = headers["X-Correlation-ID"].ToString();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        value = headers["X_CORRELATION_ID"].ToString();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        return Guid.NewGuid().ToString();
    }
}