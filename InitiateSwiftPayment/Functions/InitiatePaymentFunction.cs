using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using InitiateSwiftPayment.Models;
using InitiateSwiftPayment.Helpers;

namespace InitiateSwiftPayment.Functions
{
    public class InitiatePaymentFunction
    {
        private readonly ILogger _logger;
        private readonly DbHelper _dbHelper;
        private readonly JsonSerializerOptions _jsonOptions;

        public InitiatePaymentFunction(ILoggerFactory loggerFactory, DbHelper dbHelper)
        {
            _logger = loggerFactory.CreateLogger<InitiatePaymentFunction>();
            _dbHelper = dbHelper;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        [Function("InitiatePayment")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "payments/initiate")] HttpRequestData req)
        {
            _logger.LogInformation("Enter InitiatePayment - {Time}", DateTime.UtcNow);
            try
            {
                // Header validation
                req.Headers.TryGetValues("client_id", out var clientIdVals);
                var clientId = clientIdVals == null ? string.Empty : string.Join("", clientIdVals);

                if (string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128 || !Regex.IsMatch(clientId, "^[\\w\\+]+$"))
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Message = "Invalid or missing header: client_id" };
                    resp400.Headers.Add("Content-Type", "application/json");
                    await resp400.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Missing/invalid client_id header");
                    return resp400;
                }

                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeVals) || string.Join("", contentTypeVals).IndexOf("application/json", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    var resp415 = req.CreateResponse(HttpStatusCode.UnsupportedMediaType);
                    var err = new ErrorResponse { Message = "Content-Type must be application/json" };
                    resp415.Headers.Add("Content-Type", "application/json");
                    await resp415.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Invalid Content-Type");
                    return resp415;
                }

                req.Headers.TryGetValues("X-Correlation-Cust-Id", out var correlationVals);
                var correlationId = correlationVals == null ? Guid.NewGuid().ToString() : string.Join("", correlationVals);
                if (!string.IsNullOrEmpty(correlationId) && correlationId.Length > 100)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Message = "Header X-Correlation-Cust-Id exceeds max length 100" };
                    resp400.Headers.Add("Content-Type", "application/json");
                    await resp400.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Invalid X-Correlation-Cust-Id");
                    return resp400;
                }

                if (!req.Headers.TryGetValues("Idempotency-Key", out var idemVals) || string.IsNullOrWhiteSpace(string.Join("", idemVals)))
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Message = "Missing Idempotency-Key header" };
                    resp400.Headers.Add("Content-Type", "application/json");
                    await resp400.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Missing Idempotency-Key");
                    return resp400;
                }

                var idempotencyKeyRaw = string.Join("", idemVals);
                if (!Guid.TryParse(idempotencyKeyRaw, out var idempotencyGuid))
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Message = "Idempotency-Key must be a valid UUID" };
                    resp400.Headers.Add("Content-Type", "application/json");
                    await resp400.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Invalid Idempotency-Key");
                    return resp400;
                }

                // Read body
                string bodyText;
                using (var reader = new StreamReader(req.Body))
                {
                    bodyText = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(bodyText))
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Message = "Request body is required" };
                    resp400.Headers.Add("Content-Type", "application/json");
                    await resp400.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Empty body");
                    return resp400;
                }

                PaymentRequest paymentRequest;
                try
                {
                    paymentRequest = JsonSerializer.Deserialize<PaymentRequest>(bodyText, _jsonOptions) ?? new PaymentRequest();
                }
                catch (Exception ex)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Message = "Malformed JSON body", Details = ex.Message };
                    resp400.Headers.Add("Content-Type", "application/json");
                    await resp400.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning(ex, "JSON deserialization failed");
                    return resp400;
                }

                // Business validations
                var validationErrors = ValidationHelper.ValidatePaymentRequest(paymentRequest);
                if (validationErrors.Count > 0)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ValidationErrorResponse { Message = "Validation failed", Errors = validationErrors };
                    resp400.Headers.Add("Content-Type", "application/json");
                    await resp400.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Validation errors: {Count}", validationErrors.Count);
                    return resp400;
                }

                // Check debtor IBAN via DB or algorithm already done in ValidationHelper

                // Check BIC codes exist in BIC directory
                var debtorBicExists = await _dbHelper.CheckBicExistsAsync(paymentRequest.DebtorBic);
                if (!debtorBicExists)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Message = "Debtor BIC not found in BIC directory" };
                    resp400.Headers.Add("Content-Type", "application/json");
                    await resp400.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Debtor BIC not found: {BIC}", paymentRequest.DebtorBic);
                    return resp400;
                }

                var creditorBicExists = await _dbHelper.CheckBicExistsAsync(paymentRequest.CreditorBic);
                if (!creditorBicExists)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Message = "Creditor BIC not found in BIC directory" };
                    resp400.Headers.Add("Content-Type", "application/json");
                    await resp400.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Creditor BIC not found: {BIC}", paymentRequest.CreditorBic);
                    return resp400;
                }

                // Check requestedExecutionDate not in past
                if (paymentRequest.RequestedExecutionDate.Date < DateTime.UtcNow.Date)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Message = "Requested execution date cannot be in the past" };
                    resp400.Headers.Add("Content-Type", "application/json");
                    await resp400.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("RequestedExecutionDate in past: {Date}", paymentRequest.RequestedExecutionDate);
                    return resp400;
                }

                // Check endToEndId uniqueness for debtor account
                var isUnique = await _dbHelper.IsEndToEndUniqueAsync(paymentRequest.DebtorAccountIban, paymentRequest.EndToEndId);
                if (!isUnique)
                {
                    var resp409 = req.CreateResponse(HttpStatusCode.Conflict);
                    var err = new ErrorResponse { Message = "endToEndId is not unique for the debtor account" };
                    resp409.Headers.Add("Content-Type", "application/json");
                    await resp409.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Duplicate endToEndId for debtor: {E2E}", paymentRequest.EndToEndId);
                    return resp409;
                }

                // Check sufficient funds via CBS (via DB helper)
                var hasFunds = await _dbHelper.CheckSufficientFundsAsync(paymentRequest.DebtorAccountIban, paymentRequest.PaymentAmount, paymentRequest.PaymentCurrency);
                if (!hasFunds)
                {
                    var resp402 = req.CreateResponse(HttpStatusCode.PaymentRequired);
                    var err = new ErrorResponse { Message = "Insufficient funds in debtor account" };
                    resp402.Headers.Add("Content-Type", "application/json");
                    await resp402.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Insufficient funds for IBAN {IBAN}", paymentRequest.DebtorAccountIban);
                    return resp402;
                }

                // Currency corridor check - basic check via DB helper
                var corridorSupported = await _dbHelper.IsCurrencySupportedForCorridorAsync(paymentRequest.DebtorBic, paymentRequest.CreditorBic, paymentRequest.PaymentCurrency);
                if (!corridorSupported)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Message = "Currency not supported for the BIC corridor" };
                    resp400.Headers.Add("Content-Type", "application/json");
                    await resp400.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Currency {Currency} not supported for corridor {Debtor}-{Creditor}", paymentRequest.PaymentCurrency, paymentRequest.DebtorBic, paymentRequest.CreditorBic);
                    return resp400;
                }

                // Idempotency: check existing idempotency key
                var existsByIdempotency = await _dbHelper.ExistsByIdempotencyKeyAsync(idempotencyGuid);
                if (existsByIdempotency)
                {
                    var resp409 = req.CreateResponse(HttpStatusCode.Conflict);
                    var err = new ErrorResponse { Message = "Duplicate request: idempotency key already used" };
                    resp409.Headers.Add("Content-Type", "application/json");
                    await resp409.WriteStringAsync(JsonSerializer.Serialize(err, _jsonOptions));
                    _logger.LogWarning("Duplicate idempotency key {Key}", idempotencyGuid);
                    return resp409;
                }

                // Generate swift msg ref
                var swiftMsgRef = $"FT{DateTime.UtcNow:yyMMddHHmmss}{Guid.NewGuid().ToString("N").Substring(0,5).ToUpperInvariant()}";
                var paymentId = $"pay_{Guid.NewGuid():D}";

                // Insert payment
                var insertResult = await _dbHelper.InsertPaymentAsync(paymentRequest, idempotencyGuid, swiftMsgRef);

                var resp = req.CreateResponse(HttpStatusCode.Accepted);
                resp.Headers.Add("Content-Type", "application/json");

                var responseBody = new PaymentResponse
                {
                    PaymentId = paymentId,
                    SwiftMsgRef = swiftMsgRef,
                    Status = insertResult.Status,
                    SubmittedAt = insertResult.SubmittedAt
                };

                await resp.WriteStringAsync(JsonSerializer.Serialize(responseBody, _jsonOptions));

                _logger.LogInformation("Exit InitiatePayment - {Time}", DateTime.UtcNow);
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in InitiatePayment");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                resp.Headers.Add("Content-Type", "application/json");
                var err = new ErrorResponse { Message = "Internal server error", Details = ex.Message };
                await resp.WriteStringAsync(JsonSerializer.Serialize(err, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                return resp;
            }
        }
    }
}
