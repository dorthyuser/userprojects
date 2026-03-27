using System;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using InitiateSwiftPayment.Models;
using InitiateSwiftPayment.Helpers;
using System.Numerics;
using System.Collections.Generic;

namespace InitiateSwiftPayment.Services
{
    public class PaymentService
    {
        private readonly DbHelper _dbHelper;
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly HashSet<string> _bicDirectory = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DEUTDEFF",
            "BOFAUS3N",
            "NEDSZAJJ",
            "HSBCGB2L",
            "BNPAFRPP"
        };

        public PaymentService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<(bool IsSuccess, HttpStatusCode StatusCode, PaymentResponse? Response, ErrorResponse? Error)> ProcessPaymentAsync(PaymentRequest request, Guid idempotencyKey, string clientId, string correlationId, ILogger logger)
        {
            logger.LogInformation("Enter PaymentService.ProcessPaymentAsync");
            try
            {
                // Validate fields
                var validation = ValidateRequest(request);
                if (!validation.IsValid)
                {
                    return (false, HttpStatusCode.BadRequest, null, new ErrorResponse { Code = "ValidationError", Message = validation.Message });
                }

                // Business validations
                if (request.PaymentAmount < 0.01m || request.PaymentAmount > 999999999.99m)
                {
                    return (false, HttpStatusCode.BadRequest, null, new ErrorResponse { Code = "ValidationError", Message = "paymentAmount must be between 0.01 and 999999999.99" });
                }

                if (!IsValidIban(request.DebtorAccountIBAN))
                {
                    return (false, HttpStatusCode.BadRequest, null, new ErrorResponse { Code = "ValidationError", Message = "Invalid debtorAccountIBAN" });
                }

                if (!IsValidBic(request.DebtorBIC) || !_bicDirectory.Contains(request.DebtorBIC))
                {
                    return (false, HttpStatusCode.BadRequest, null, new ErrorResponse { Code = "ValidationError", Message = "Debtor BIC not found in directory or invalid format" });
                }

                if (!IsValidBic(request.CreditorBIC) || !_bicDirectory.Contains(request.CreditorBIC))
                {
                    return (false, HttpStatusCode.BadRequest, null, new ErrorResponse { Code = "ValidationError", Message = "Creditor BIC not found in directory or invalid format" });
                }

                if (request.RequestedExecutionDate.Date < DateTime.UtcNow.Date)
                {
                    return (false, HttpStatusCode.BadRequest, null, new ErrorResponse { Code = "ValidationError", Message = "requestedExecutionDate cannot be in the past" });
                }

                // Unique endToEndId for debtor account
                var exists = await _dbHelper.EndToEndIdExistsAsync(request.EndToEndId, request.DebtorAccountIBAN);
                if (exists)
                {
                    return (false, HttpStatusCode.Conflict, null, new ErrorResponse { Code = "DuplicateEndToEndId", Message = "endToEndId already exists for the debtor account" });
                }

                // Check funds via CBS
                var balance = await _dbHelper.GetAccountBalanceAsync(request.DebtorAccountIBAN);
                if (balance < request.PaymentAmount)
                {
                    return (false, HttpStatusCode.BadRequest, null, new ErrorResponse { Code = "InsufficientFunds", Message = "Insufficient funds in debtor account" });
                }

                // Currency corridor check (simple example: only same currency for both BICs for this sample)
                // In production, this would call a service
                if (!IsSupportedCurrency(request.PaymentCurrency))
                {
                    return (false, HttpStatusCode.BadRequest, null, new ErrorResponse { Code = "UnsupportedCurrency", Message = "Currency not supported for the given BIC corridor" });
                }

                // All checks passed, create swift ref and insert
                var swiftRef = GenerateSwiftRef();
                var insertRes = await _dbHelper.InsertPaymentAsync(request, idempotencyKey, swiftRef);

                var response = new PaymentResponse
                {
                    PaymentId = $"pay_{Guid.NewGuid().ToString()}",
                    SwiftMsgRef = swiftRef,
                    Status = "ACCEPTED",
                    SubmittedAt = insertRes.SubmittedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                logger.LogInformation("Payment accepted: {PaymentId}", response.PaymentId);
                return (true, HttpStatusCode.Accepted, response, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ProcessPaymentAsync");
                return (false, HttpStatusCode.InternalServerError, null, new ErrorResponse { Code = "InternalError", Message = "An internal error occurred", Details = ex.Message });
            }
            finally
            {
                logger.LogInformation("Exit PaymentService.ProcessPaymentAsync");
            }
        }

        private bool IsSupportedCurrency(string currency)
        {
            // Very basic support: accept standard 3-letter codes
            return Regex.IsMatch(currency, "^[A-Z]{3}$");
        }

        private string GenerateSwiftRef()
        {
            // Example format FT + random alphanumeric
            var rnd = Guid.NewGuid().ToString("N").Substring(0, 13).ToUpperInvariant();
            return $"FT{DateTime.UtcNow:yyMMdd}{rnd}";
        }

        private (bool IsValid, string Message) ValidateRequest(PaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PaymentCurrency)) return (false, "paymentCurrency is required");
            if (string.IsNullOrWhiteSpace(request.DebtorAccountIBAN)) return (false, "debtorAccountIBAN is required");
            if (string.IsNullOrWhiteSpace(request.DebtorBIC)) return (false, "debtorBIC is required");
            if (string.IsNullOrWhiteSpace(request.CreditorAccountIBAN)) return (false, "creditorAccountIBAN is required");
            if (string.IsNullOrWhiteSpace(request.CreditorBIC)) return (false, "creditorBIC is required");
            if (string.IsNullOrWhiteSpace(request.CreditorName)) return (false, "creditorName is required");
            if (string.IsNullOrWhiteSpace(request.EndToEndId)) return (false, "endToEndId is required");
            if (request.PaymentAmount < 0.01m) return (false, "paymentAmount must be >= 0.01");
            if (request.PaymentType == BankSwiftPaymentTypeEnum.MT103 || request.PaymentType == BankSwiftPaymentTypeEnum.MT202 || request.PaymentType == BankSwiftPaymentTypeEnum.pacs_008 || request.PaymentType == BankSwiftPaymentTypeEnum.pacs_009)
            {
                // ok
            }
            else
            {
                return (false, "Unsupported paymentType");
            }

            if (request.DebtorAccountIBAN.Length < 15 || request.DebtorAccountIBAN.Length > 34) return (false, "debtorAccountIBAN length invalid");
            if (request.CreditorAccountIBAN.Length < 15 || request.CreditorAccountIBAN.Length > 34) return (false, "creditorAccountIBAN length invalid");
            if (request.CreditorName.Length > 140) return (false, "creditorName exceeds max length");
            if (!string.IsNullOrEmpty(request.RemittanceInfo) && request.RemittanceInfo.Length > 140) return (false, "remittanceInfo exceeds max length");
            if (request.EndToEndId.Length > 35) return (false, "endToEndId exceeds max length");
            return (true, string.Empty);
        }

        private bool IsValidBic(string bic)
        {
            if (string.IsNullOrWhiteSpace(bic)) return false;
            var regex = new Regex("^[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?$");
            return regex.IsMatch(bic);
        }

        private bool IsValidIban(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return false;
            var cleaned = iban.Replace(" ", string.Empty).ToUpperInvariant();
            if (cleaned.Length < 15 || cleaned.Length > 34) return false;
            // Move first four chars to end
            var rearranged = cleaned.Substring(4) + cleaned.Substring(0, 4);
            // Convert letters to numbers
            var sb = new System.Text.StringBuilder();
            foreach (var ch in rearranged)
            {
                if (char.IsLetter(ch))
                {
                    int val = ch - 'A' + 10;
                    sb.Append(val.ToString());
                }
                else if (char.IsDigit(ch))
                {
                    sb.Append(ch);
                }
                else return false;
            }

            // Perform mod-97
            var numericString = sb.ToString();
            const int chunkSize = 9; // process in chunks to avoid huge BigInteger allocations
            int start = 0;
            int len = numericString.Length;
            int rem = 0;
            while (start < len)
            {
                int take = Math.Min(chunkSize, len - start);
                var part = rem.ToString() + numericString.Substring(start, take);
                rem = (int)(BigInteger.Parse(part) % 97);
                start += take;
            }
            return rem == 1;
        }
    }
}
