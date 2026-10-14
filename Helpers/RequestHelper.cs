using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;

namespace BusTravelAccountsLambda;

public static class RequestHelper
{
    public static string GetHeader(IDictionary<string, string>? headers, string name)
    {
        if (headers is null)
        {
            return string.Empty;
        }

        foreach (var pair in headers)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return string.Empty;
    }

    public static string GetCorrelationId(IDictionary<string, string>? headers)
    {
        var correlation = GetHeader(headers, "X-Correlation-ID");
        return string.IsNullOrWhiteSpace(correlation) ? Guid.NewGuid().ToString() : correlation.Trim();
    }

    public static string GetCustomerCorrelationId(IDictionary<string, string>? headers)
    {
        var value = GetHeader(headers, "CUSTOMER_CORRELATION_ID");
        return string.IsNullOrWhiteSpace(value) ? "CUSTOMER_CORRELATION_ID_NOT_FOUND" : value.Trim();
    }

    public static string? GetQuery(IDictionary<string, string>? query, string name)
    {
        if (query is null)
        {
            return null;
        }

        foreach (var pair in query)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }

    public static string? GetBody(APIGatewayProxyRequest request)
    {
        if (request.Body is null)
        {
            return null;
        }

        if (!request.IsBase64Encoded)
        {
            return request.Body;
        }

        var bytes = Convert.FromBase64String(request.Body);
        return Encoding.UTF8.GetString(bytes);
    }

    public static string StripBasePath(string path, string basePath)
    {
        return BasePath.Strip(path, basePath);
    }

    public static IDictionary<string, string> Headers(string correlationId, string customerCorrelationId)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Correlation-ID"] = correlationId,
            ["CUSTOMER_CORRELATION_ID"] = customerCorrelationId
        };
    }

    public static string Serialize(object value) => JsonSerializer.Serialize(value, JsonHelper.Options);
}