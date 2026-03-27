using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InitiateSwiftPayment.Helpers;
using InitiateSwiftPayment.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace InitiateSwiftPayment.Services
{
    public class PaymentService
    {
        private readonly DbHelper _dbHelper;
        private readonly BicDirectory _bicDirectory;
        private readonly CbsClient _cbsClient;
        private readonly ILogger _logger;

        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly Regex ClientIdRegex = new Regex("^[\\w+]+$", RegexOptions.Compiled);
        private static readonly Regex CorrelationRegex = new Regex("^[A-Za-z0-9_-]{0,100}$", RegexOptions.Compiled);
        private static readonly Regex IsoCurrency = new Regex("^[A-Z]{3}$", RegexOptions.Compiled);
        private static readonly Regex IdempotencyUuid = new Regex("^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);

        public PaymentService(DbHelper dbHelper, BicDirectory bicDirectory, CbsClient cbsClient, ILoggerFactory loggerFactory)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
            _bicDirectory = bicDirectory ?? throw new ArgumentNullException(nameof(bicDirectory));
            _cbsClient = cbsClient ?? throw new ArgumentNullException(nameof(cbsClient));
            _logger = loggerFactory.CreateLogger<PaymentService>();
        }

        public async Task<ServiceResult> ProcessPaymentAsync(InitiateSwiftPaymentRequest dto, string clientId, string idempotencyKeyHeader, string correlationId)
        {
            _logger.LogInformation("Enter ProcessPaymentAsync");

            var errors = new List<string>();

            // Headers
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128 || !ClientIdRegex.IsMatch(clientId))
            {
                errors.Add("Invalid or missing client_id header");
            }

            if (string.IsNullOrWhiteSpace(idempotencyKeyHeader) || !IdempotencyUuid.IsMatch(idempotencyKeyHeader))
            {
                errors.Add("Missing or invalid Idempotency-Key header (must be UUID v4 format)");
            }

            if (!string.IsNullOrEmpty(correlationId) && !CorrelationRegex.IsMatch(correlationId))
            {
                errors.Add("Invalid X-Correlation-Cust-Id header");
            }

            // Body validations
            if (dto.PaymentAmount < 0.01M || dto.PaymentAmount > 999999999.99M)
            {
                errors.Add("paymentAmount must be between 0.01 and 999999999.99");
            }

            if (!IsoCurrency.IsMatch(dto.PaymentCurrency))
            {
                errors.Add("paymentCurrency must be ISO 4217 (3 uppercase letters)");
            }

            if (string.IsNullOrWhiteSpace(dto.DebtorAccountIBAN) || dto.DebtorAccountIBAN.Length < 15 || dto.DebtorAccountIBAN.Length > 34 || !IbanValidator.Validate(dto.DebtorAccountIBAN))
            {
                errors.Add("Invalid debtorAccountIBAN");
            }

            if (string.IsNullOrWhiteSpace(dto.CreditorAccountIBAN) || dto.CreditorAccountIBAN.Length < 15 || dto.CreditorAccountIBAN.Length > 34 || !IbanValidator.Validate(dto.CreditorAccountIBAN))
            {
                errors.Add("Invalid creditorAccountIBAN");
            }

            if (string.IsNullOrWhiteSpace(dto.DebtorBIC) || !_bicDirectory.IsValidFormat(dto.DebtorBIC) || !_bicDirectory.Exists(dto.DebtorBIC))
            {
                errors.Add("Invalid or unknown debtorBIC");
            }

            if (string.IsNullOrWhiteSpace(dto.CreditorBIC) || !_bicDirectory.IsValidFormat(dto.CreditorBIC) || !_bicDirectory.Exists(dto.CreditorBIC))
            {
                errors.Add("Invalid or unknown creditorBIC");
            }

            if (string.IsNullOrWhiteSpace(dto.CreditorName) || dto.CreditorName.Length > 140)
            {
                errors.Add("Invalid creditorName");
            }

            if (!string.IsNullOrEmpty(dto.RemittanceInfo) && dto.RemittanceInfo.Length > 140)
            {
                errors.Add("remittanceInfo must be <= 140 characters");
            }

            if (dto.RequestedExecutionDate.Date < DateTime.UtcNow.Date)
            {
                errors.Add("requestedExecutionDate cannot be in the past");
            }

            if (string.IsNullOrWhiteSpace(dto.EndToEndId) || dto.EndToEndId.Length > 35)
            {
                errors.Add("Invalid endToEndId");
            }

            if (errors.Count > 0)
            {
                _logger.LogWarning("Validation errors: {Count}", errors.Count);
                var err = new ErrorResponse { Error = "Validation Failed", Message = "One or more validation errors occurred", Details = errors };
                return ServiceResult.Fail(err, (int)HttpStatusCode.BadRequest);
            }

            // Idempotency: check existing
            var idempotencyGuid = Guid.Parse(idempotencyKeyHeader);
            var existing = await _dbHelper.GetByIdempotencyKeyAsync(idempotencyGuid);
            if (existing.Found && existing.Existing is not null)
            {
                var err = new ErrorResponse { Error = "Conflict", Message = "Request with same idempotency key already processed", Details = new List<string> { "Payment already exists" } };
                return ServiceResult.Fail(err, (int)HttpStatusCode.Conflict, existing.Existing);
            }

            // EndToEnd uniqueness for debtor
            var endToEndExists = await _dbHelper.EndToEndExistsAsync(dto.DebtorAccountIBAN, dto.EndToEndId);
            if (endToEndExists)
            {
                var err = new ErrorResponse { Error = "Conflict", Message = "endToEndId already exists for debtor account", Details = new List<string> { "endToEndId must be unique per debtor account" } };
                return ServiceResult.Fail(err, (int)HttpStatusCode.Conflict);
            }

            // Currency corridor check
            if (!_bicDirectory.IsCurrencySupported(dto.DebtorBIC, dto.CreditorBIC, dto.PaymentCurrency))
            {
                var err = new ErrorResponse { Error = "Unsupported Currency", Message = "Currency not supported for given BIC corridor", Details = new List<string> { "Currency corridor not supported" } };
                return ServiceResult.Fail(err, (int)HttpStatusCode.BadRequest);
            }

            // Sufficient funds via CBS
            var hasFunds = await _cbsClient.HasSufficientFundsAsync(dto.DebtorAccountIBAN, dto.PaymentAmount, dto.PaymentCurrency);
            if (!hasFunds)
            {
                var err = new ErrorResponse { Error = "Insufficient Funds", Message = "Debtor account does not have sufficient funds", Details = new List<string> { "Insufficient funds" } };
                return ServiceResult.Fail(err, (int)HttpStatusCode.PaymentRequired);
            }

            // Compose swift message ref
            var swiftMsgRef = GenerateSwiftReference();

            // Insert into DB with status ACCEPTED
            try
            {
                var resp = await _dbHelper.InsertPaymentAsync(dto, idempotencyGuid, swiftMsgRef, BankPaymentStatus.ACCEPTED);
                _logger.LogInformation("Payment inserted successfully");
                return ServiceResult.Succeed(resp);
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "23505")
            {
                // Unique constraint violation (idempotency)
                var err = new ErrorResponse { Error = "Conflict", Message = "Duplicate idempotency key", Details = new List<string> { pgEx.Message } };
                _logger.LogWarning(pgEx, "Postgres unique constraint violation");
                return ServiceResult.Fail(err, (int)HttpStatusCode.Conflict);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert payment");
                var err = new ErrorResponse { Error = "InternalServerError", Message = "Failed to process payment", Details = new List<string> { ex.Message } };
                return ServiceResult.Fail(err, (int)HttpStatusCode.InternalServerError);
            }
        }

        private string GenerateSwiftReference()
        {
            // Simple deterministic-ish ref for demo
            return "FT" + DateTime.UtcNow.ToString("yyMMddHHmmss") + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
        }

        public async Task<HttpResponseData> CreateErrorResponseAsync(HttpRequestData req, HttpStatusCode status, string message)
        {
            var resp = req.CreateResponse(status);
            resp.Headers.Add("Content-Type", "application/json");
            var err = new ErrorResponse { Error = status.ToString(), Message = message };
            await resp.WriteStringAsync(JsonSerializer.Serialize(err, JsonOptions));
            return resp;
        }
    }

    public class ServiceResult
    {
        public bool Success { get; set; }
        public InitiateSwiftPaymentResponse? Response { get; set; }
        public ErrorResponse? Error { get; set; }
        public int StatusCode { get; set; }

        public static ServiceResult Fail(ErrorResponse err, int statusCode, InitiateSwiftPaymentResponse? existing = null)
        {
            return new ServiceResult { Success = false, Error = err, StatusCode = statusCode, Response = existing };
        }

        public static ServiceResult Succeed(InitiateSwiftPaymentResponse resp)
        {
            return new ServiceResult { Success = true, Response = resp, StatusCode = (int)System.Net.HttpStatusCode.Created };
        }
    }
}
