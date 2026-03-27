using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using InitiateSwiftPayment.Services;
using InitiateSwiftPayment.Models;

namespace InitiateSwiftPayment.Functions
{
    public class InitiateSwiftPaymentFunction
    {
        private readonly PaymentService _paymentService;

        public InitiateSwiftPaymentFunction(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [Function("InitiateSwiftPayment")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "initiate-swift-payment")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("InitiateSwiftPayment");
            logger.LogInformation("Enter InitiateSwiftPaymentFunction");
            try
            {
                // Header validation existence
                if (!req.Headers.TryGetValues("client_id", out var clientIdValues))
                {
                    var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Code = "MissingHeader", Message = "Missing required header: client_id" };
                    await resp.WriteAsJsonAsync(err);
                    logger.LogWarning("Missing client_id header");
                    return resp;
                }

                string clientId = string.Empty;
                foreach (var v in clientIdValues) { clientId = v; break; }

                // Content-Type header required
                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
                {
                    var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Code = "MissingHeader", Message = "Missing required header: Content-Type" };
                    await resp.WriteAsJsonAsync(err);
                    logger.LogWarning("Missing Content-Type header");
                    return resp;
                }

                if (!req.Headers.TryGetValues("Idempotency-Key", out var idempotencyValues))
                {
                    var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Code = "MissingHeader", Message = "Missing required header: Idempotency-Key" };
                    await resp.WriteAsJsonAsync(err);
                    logger.LogWarning("Missing Idempotency-Key header");
                    return resp;
                }

                string idempotencyKeyRaw = string.Empty;
                foreach (var v in idempotencyValues) { idempotencyKeyRaw = v; break; }
                if (!Guid.TryParse(idempotencyKeyRaw, out var idempotencyKey))
                {
                    var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Code = "InvalidHeader", Message = "Idempotency-Key must be a valid UUID v4" };
                    await resp.WriteAsJsonAsync(err);
                    logger.LogWarning("Invalid Idempotency-Key: {Key}", idempotencyKeyRaw);
                    return resp;
                }

                // Optional correlation header
                string correlationId = string.Empty;
                if (req.Headers.TryGetValues("X-Correlation-Cust-Id", out var corrValues))
                {
                    foreach (var v in corrValues) { correlationId = v; break; }
                }

                // Read body
                PaymentRequest requestDto;
                try
                {
                    requestDto = await JsonSerializer.DeserializeAsync<PaymentRequest>(req.Body, PaymentService.JsonOptions);
                    if (requestDto == null)
                    {
                        var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                        var err = new ErrorResponse { Code = "InvalidBody", Message = "Request body is empty or invalid" };
                        await resp.WriteAsJsonAsync(err);
                        logger.LogWarning("Empty or invalid body");
                        return resp;
                    }
                }
                catch (JsonException jex)
                {
                    var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Code = "InvalidJson", Message = "Malformed JSON", Details = jex.Message };
                    await resp.WriteAsJsonAsync(err);
                    logger.LogWarning(jex, "JSON parse error");
                    return resp;
                }

                // Call service to process
                var result = await _paymentService.ProcessPaymentAsync(requestDto, idempotencyKey, clientId, correlationId, logger);
                if (!result.IsSuccess)
                {
                    var resp = req.CreateResponse(result.StatusCode);
                    await resp.WriteAsJsonAsync(result.Error);
                    logger.LogWarning("Payment processing failed: {Code} - {Message}", result.Error.Code, result.Error.Message);
                    return resp;
                }

                var successResp = req.CreateResponse(HttpStatusCode.Accepted);
                await successResp.WriteAsJsonAsync(result.Response!);
                logger.LogInformation("Exit InitiateSwiftPaymentFunction with success, paymentId: {PaymentId}", result.Response!.PaymentId);
                return successResp;
            }
            catch (Exception ex)
            {
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                var err = new ErrorResponse { Code = "InternalError", Message = "An unexpected error occurred", Details = ex.Message };
                await resp.WriteAsJsonAsync(err);
                logger.LogError(ex, "Unhandled exception in InitiateSwiftPaymentFunction");
                return resp;
            }
        }
    }
}
