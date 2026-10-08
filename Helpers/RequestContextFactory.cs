namespace BusTravelAccountsSaMainLambda;

public static class RequestContextFactory
{
    public static BasicDetails Create(APIGatewayProxyRequest request)
    {
        var customerCorrelationId = HeaderHelper.GetHeaderValue(request.Headers, "CUSTOMER_CORRELATION_ID");
        var xCorrelationId = HeaderHelper.GetHeaderValue(request.Headers, "X_CORRELATION_ID");
        var clientId = HeaderHelper.GetHeaderValue(request.Headers, "client_id");

        return new BasicDetails
        {
            CustomerCorrelationId = customerCorrelationId is null ? "CUSTOMER_CORRELATION_ID_NOT_FOUND" : customerCorrelationId.Trim(),
            XCorrelationId = string.IsNullOrWhiteSpace(xCorrelationId) ? Guid.NewGuid().ToString() : xCorrelationId.Trim(),
            ClientId = clientId?.Trim(),
            HttpMethod = request.HttpMethod,
            RelativePath = request.Path
        };
    }
}