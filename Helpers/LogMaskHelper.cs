using System.Text.Json;

namespace BusTravelAccountsSaMainLambda;

public static class LogMaskHelper
{
    public static string SerializeBasicDetails(BasicDetails details)
    {
        var safe = new
        {
            CUSTOMER_CORRELATION_ID = details.CustomerCorrelationId,
            X_CORRELATION_ID = details.XCorrelationId,
            client_id = details.ClientId,
            httpMethod = details.HttpMethod,
            relativePath = details.RelativePath
        };

        return JsonSerializer.Serialize(safe, JsonOptionsHelper.Options);
    }

    public static string MaskPayload(string? payload)
    {
        return string.IsNullOrWhiteSpace(payload) ? "null" : "***MASKED***";
    }

    public static string SerializeCreateOrUpdateRequest(object request, string? id)
    {
        var safe = new
        {
            id = id is null ? null : MaskId(id),
            payload = "***MASKED***"
        };

        return JsonSerializer.Serialize(safe, JsonOptionsHelper.Options);
    }

    public static string MaskId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return string.Empty;
        }

        return "***MASKED***";
    }
}