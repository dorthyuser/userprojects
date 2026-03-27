using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using InitiateSwiftPayment.Helpers;
using InitiateSwiftPayment.Models;
using System.Net;
using System.Collections.Generic;

namespace InitiateSwiftPayment.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, string clientId, string correlationId);
    }

    public class ServiceException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ErrorCode { get; }

        public ServiceException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, string errorCode = "BUSINESS_VALIDATION_FAILED") : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }

    public class PaymentService : IPaymentService
    {
        private readonly Helpers.DbHelper _dbHelper;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(Helpers.DbHelper dbHelper, ILogger<PaymentService> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, string clientId, string correlationId)
        {
            _logger.LogInformation("Entered PaymentService.ProcessPaymentAsync for client {Client} correlation {Corr} at {Time}", clientId, correlationId, DateTime.UtcNow);

            // Basic validations
            if (request.PaymentAmount < 0.01M || request.PaymentAmount > 999999999.99M)
            {
                throw new ServiceException("paymentAmount must be between 0.01 and 999999999.99");
            }

            if (!Validators.IsValidIban(request.DebtorAccountIBAN))
            {
                throw new ServiceException("Invalid debtorAccountIBAN");
            }

            if (!Validators.IsValidIban(request.CreditorAccountIBAN))
            {
                throw new ServiceException("Invalid creditorAccountIBAN");
            }

            if (!Validators.IsValidBic(request.DebtorBIC))
            {
                throw new ServiceException("Invalid debtorBIC");
            }

            if (!Validators.IsValidBic(request.CreditorBIC))
            {
                throw new ServiceException("Invalid creditorBIC");
            }

            if (!Validators.IsValidCurrency(request.PaymentCurrency))
            {
                throw new ServiceException("Invalid paymentCurrency");
            }

            if (request.RequestedExecutionDate.Date < DateTime.UtcNow.Date)
            {
                throw new ServiceException("requestedExecutionDate cannot be in the past");
            }

            if (string.IsNullOrWhiteSpace(request.EndToEndId) || request.EndToEndId.Length > 35)
            {
                throw new ServiceException("Invalid endToEndId");
            }

            // Check uniqueness of endToEndId for debtor
            var exists = await _dbHelper.EndToEndExistsAsync(request.DebtorAccountIBAN, request.EndToEndId);
            if (exists)
            {
                throw new ServiceException("endToEndId already exists for debtor account", HttpStatusCode.Conflict, "END_TO_END_ID_DUPLICATE");
            }

            // Check sufficient funds - placeholder integration
            if (!await HasSufficientFundsAsync(request.DebtorAccountIBAN, request.PaymentAmount))
            {
                throw new ServiceException("Insufficient funds", HttpStatusCode.PaymentRequired, "INSUFFICIENT_FUNDS");
            }

            // Check currency corridor - placeholder
            if (!IsCurrencySupportedForCorridor(request.DebtorBIC, request.CreditorBIC, request.PaymentCurrency))
            {
                throw new ServiceException("Currency not supported for corridor between provided BICs");
            }

            // Prepare record
            var record = new PaymentRecord
            {
                PaymentType = request.PaymentType,
                PaymentAmount = request.PaymentAmount,
                PaymentCurrency = request.PaymentCurrency,
                DebtorAccountIBAN = request.DebtorAccountIBAN,
                DebtorBIC = request.DebtorBIC,
                CreditorAccountIBAN = request.CreditorAccountIBAN,
                CreditorBIC = request.CreditorBIC,
                CreditorName = request.CreditorName,
                RemittanceInfo = request.RemittanceInfo,
                RequestedExecutionDate = request.RequestedExecutionDate.Date,
                EndToEndId = request.EndToEndId,
                SwiftMsgRef = GenerateSwiftMsgRef(),
                Status = BankPaymentStatusEnum.ACCEPTED,
                SubmittedAt = DateTime.UtcNow,
                IdempotencyKey = request.IdempotencyKey
            };

            try
            {
                var inserted = await _dbHelper.InsertPaymentAsync(record);

                var response = new PaymentResponse
                {
                    PaymentId = "pay_" + Guid.NewGuid().ToString(),
                    SwiftMsgRef = string.IsNullOrWhiteSpace(inserted.SwiftMsgRef) ? record.SwiftMsgRef ?? string.Empty : inserted.SwiftMsgRef,
                    Status = inserted.Status.ToString(),
                    SubmittedAt = inserted.SubmittedAt
                };

                _logger.LogInformation("Payment accepted with PaymentId {PaymentId}", response.PaymentId);
                _logger.LogInformation("Exiting PaymentService.ProcessPaymentAsync at {Time}", DateTime.UtcNow);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting payment record");
                throw new ServiceException("Failed to persist payment: " + ex.Message, HttpStatusCode.InternalServerError, "DB_ERROR");
            }
        }

        private string GenerateSwiftMsgRef()
        {
            return "FT" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();
        }

        private Task<bool> HasSufficientFundsAsync(string debtorIban, decimal amount)
        {
            // Placeholder: In production this would call CBS. For now, disallow very large amounts to simulate insufficient funds.
            if (amount > 10000000M)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private bool IsCurrencySupportedForCorridor(string debtorBic, string creditorBic, string currency)
        {
            // Placeholder: Real implementation would check BIC directory and supported currency corridors
            // For demo accept common currencies
            var supported = new HashSet<string> { "USD", "EUR", "GBP", "CHF", "JPY" };
            return supported.Contains(currency);
        }
    }
}
