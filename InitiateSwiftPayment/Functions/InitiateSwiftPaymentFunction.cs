using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using InitiateSwiftPayment.Models;
using InitiateSwiftPayment.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace InitiateSwiftPayment.Functions
{
    public class InitiateSwiftPaymentFunction
    {
        private readonly PaymentService _paymentService;
        private readonly ILogger _logger;

        public InitiateSwiftPaymentFunction(PaymentService paymentService, ILoggerFactory loggerFactory)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _logger = loggerFactory.CreateLogger<InitiateSwiftPaymentFunction>();
        }

        [Function("InitiateSwiftPayment")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "initiate-swift-payment")] HttpRequestData req)
        {
            _logger.LogInformation("Enter InitiateSwiftPaymentFunction.Run");
            try
            {
                // Header validation
                if (!req.Headers.TryGetValues("Content-Type", out var ctVals) || ctVals is null)
                {
                    return await _paymentService.CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, "Missing Content-Type header");
                }

                var contentType = System.Linq.Enumerable.FirstOrDefault(ctVals) ?? string.Empty;
                if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    return await _paymentService.CreateErrorResponseAsync(req, HttpStatusCode.UnsupportedMediaType, "Content-Type must be application/json");
                }

                req.Headers.TryGetValues("client_id", out var clientIdVals);
                var clientId = clientIdVals is null ? string.Empty : System.Linq.Enumerable.FirstOrDefault(clientIdVals) ?? string.Empty;

                req.Headers.TryGetValues("Idempotency-Key", out var idempVals);
                var idempotencyKeyHeader = idempVals is null ? string.Empty : System.Linq.Enumerable.FirstOrDefault(idempVals) ?? string.Empty;

                req.Headers.TryGetValues("X-Correlation-Cust-Id", out var corrVals);
                var correlationId = corrVals is null ? string.Empty : System.Linq.Enumerable.FirstOrDefault(corrVals) ?? string.Empty;

                // Read body
                var body = await req.ReadAsStringAsync();
                InitiateSwiftPaymentRequest? dto = null;
                try
                {
                    dto = JsonSerializer.Deserialize<InitiateSwiftPaymentRequest>(body, PaymentService.JsonOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize request body");
                    return await _paymentService.CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, "Invalid JSON payload");
                }

                if (dto is null)
                {
                    return await _paymentService.CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, "Empty request body");
                }

                // Validate and process
                var result = await _paymentService.ProcessPaymentAsync(dto, clientId, idempotencyKeyHeader, correlationId);

                if (!result.Success)
                {
                    var status = result.StatusCode == 0 ? HttpStatusCode.BadRequest : (HttpStatusCode)result.StatusCode;
                    var resp = req.CreateResponse(status);
                    resp.Headers.Add("Content-Type", "application/json");
                    await resp.WriteStringAsync(JsonSerializer.Serialize(result.Error! , PaymentService.JsonOptions));
                    _logger.LogWarning("Validation/Processing failed: {Reason}", result.Error?.Message);
                    _logger.LogInformation("Exit InitiateSwiftPaymentFunction.Run");
                    return resp;
                }

                var response = req.CreateResponse(HttpStatusCode.Created);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(JsonSerializer.Serialize(result.Response!, PaymentService.JsonOptions));

                _logger.LogInformation("Exit InitiateSwiftPaymentFunction.Run");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in InitiateSwiftPaymentFunction");
                return await _paymentService.CreateErrorResponseAsync(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}
