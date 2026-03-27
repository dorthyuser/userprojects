using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using InitiateSwiftPayment.Models;
using InitiateSwiftPayment.Services;

namespace InitiateSwiftPayment.Functions
{
    public class InitiateSwiftPaymentFunction
    {
        private readonly ILogger<InitiateSwiftPaymentFunction> _logger;
        private readonly IPaymentService _paymentService;

        public InitiateSwiftPaymentFunction(ILogger<InitiateSwiftPaymentFunction> logger, IPaymentService paymentService)
        {
            _logger = logger;
            _paymentService = paymentService;
        }

        [Function("InitiateSwiftPayment")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "payments/swift")] HttpRequestData req)
        {
            _logger.LogInformation("Entered InitiateSwiftPaymentFunction at {Time}", DateTime.UtcNow);

            var response = req.CreateResponse();
            try
            {
                // Header validations
                if (!req.Headers.TryGetValues("client_id", out var clientIds) || string.IsNullOrWhiteSpace(clientIds?.FirstOrDefault()))
                {
                    _logger.LogWarning("Missing or empty client_id header");
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new { error = "Missing required header: client_id" });
                    return response;
                }

                var clientId = clientIds.First();
                if (clientId.Length > 128 || !Regex.IsMatch(clientId, "^[\\w+]+$"))
                {
                    _logger.LogWarning("Invalid client_id header format");
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new { error = "Invalid client_id header format" });
                    return response;
                }

                if (!req.Headers.TryGetValues("Content-Type", out var contentTypes) || string.IsNullOrWhiteSpace(contentTypes?.FirstOrDefault()))
                {
                    _logger.LogWarning("Missing Content-Type header");
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new { error = "Missing required header: Content-Type" });
                    return response;
                }

                var contentType = contentTypes.First();
                if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Unsupported Content-Type: {ContentType}", contentType);
                    response.StatusCode = System.Net.HttpStatusCode.UnsupportedMediaType;
                    await response.WriteAsJsonAsync(new { error = "Content-Type must be application/json" });
                    return response;
                }

                if (!req.Headers.TryGetValues("Idempotency-Key", out var idempVals) || string.IsNullOrWhiteSpace(idempVals?.FirstOrDefault()))
                {
                    _logger.LogWarning("Missing Idempotency-Key header");
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new { error = "Missing required header: Idempotency-Key" });
                    return response;
                }

                var idempotencyKey = idempVals.First();
                if (!Guid.TryParse(idempotencyKey, out var idempGuid) || idempGuid == Guid.Empty)
                {
                    _logger.LogWarning("Invalid Idempotency-Key format");
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new { error = "Idempotency-Key must be a valid UUID v4" });
                    return response;
                }

                string correlationId = string.Empty;
                if (req.Headers.TryGetValues("X-Correlation-Cust-Id", out var corrVals))
                {
                    correlationId = corrVals.FirstOrDefault() ?? string.Empty;
                    if (correlationId.Length > 100 || !Regex.IsMatch(correlationId, "^[A-Za-z0-9_-]+$"))
                    {
                        _logger.LogWarning("Invalid X-Correlation-Cust-Id header");
                        response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                        await response.WriteAsJsonAsync(new { error = "Invalid X-Correlation-Cust-Id header format" });
                        return response;
                    }
                }

                // Read body
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("Request body is empty");
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new { error = "Request body is required" });
                    return response;
                }

                PaymentRequest paymentRequest;
                try
                {
                    paymentRequest = JsonSerializer.Deserialize<PaymentRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new PaymentRequest();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize request body");
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new { error = "Invalid JSON in request body" });
                    return response;
                }

                // Attach idempotency from header
                paymentRequest.IdempotencyKey = idempGuid;

                var result = await _paymentService.ProcessPaymentAsync(paymentRequest, clientId, correlationId);

                response.StatusCode = System.Net.HttpStatusCode.Accepted;
                await response.WriteAsJsonAsync(result);

                _logger.LogInformation("Exiting InitiateSwiftPaymentFunction at {Time}", DateTime.UtcNow);
                return response;
            }
            catch (ServiceException sEx)
            {
                _logger.LogError(sEx, "Business validation failed");
                response.StatusCode = sEx.StatusCode;
                await response.WriteAsJsonAsync(new { error = sEx.Message, code = sEx.ErrorCode });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in InitiateSwiftPaymentFunction");
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new { error = "An unexpected error occurred" });
                return response;
            }
        }
    }
}
