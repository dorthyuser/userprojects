using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using InitiateSwiftPayment.Models;

namespace InitiateSwiftPayment.Helpers
{
    public static class ValidationHelper
    {
        public static List<string> ValidatePaymentRequest(PaymentRequest req)
        {
            var errors = new List<string>();
            if (req == null)
            {
                errors.Add("Request is null");
                return errors;
            }

            if (!Enum.IsDefined(typeof(BankSwiftPaymentTypeEnum), req.PaymentType))
            {
                errors.Add("Invalid paymentType");
            }

            if (req.PaymentAmount < 0.01m || req.PaymentAmount > 999999999.99m)
            {
                errors.Add("paymentAmount must be between 0.01 and 999999999.99");
            }

            if (string.IsNullOrWhiteSpace(req.PaymentCurrency) || !Regex.IsMatch(req.PaymentCurrency, "^[A-Z]{3}$"))
            {
                errors.Add("paymentCurrency must be a 3-letter ISO currency code in uppercase");
            }

            if (string.IsNullOrWhiteSpace(req.DebtorAccountIban) || req.DebtorAccountIban.Length < 15 || req.DebtorAccountIban.Length > 34 || !IsValidIban(req.DebtorAccountIban))
            {
                errors.Add("debtorAccountIBAN is invalid");
            }

            if (string.IsNullOrWhiteSpace(req.DebtorBic) || !Regex.IsMatch(req.DebtorBic, "^[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?$"))
            {
                errors.Add("debtorBIC is invalid");
            }

            if (string.IsNullOrWhiteSpace(req.CreditorAccountIban) || req.CreditorAccountIban.Length < 15 || req.CreditorAccountIban.Length > 34 || !IsValidIban(req.CreditorAccountIban))
            {
                errors.Add("creditorAccountIBAN is invalid");
            }

            if (string.IsNullOrWhiteSpace(req.CreditorBic) || !Regex.IsMatch(req.CreditorBic, "^[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?$"))
            {
                errors.Add("creditorBIC is invalid");
            }

            if (string.IsNullOrWhiteSpace(req.CreditorName) || req.CreditorName.Length > 140)
            {
                errors.Add("creditorName is required and must be <= 140 characters");
            }

            if (!string.IsNullOrWhiteSpace(req.RemittanceInfo) && req.RemittanceInfo.Length > 140)
            {
                errors.Add("remittanceInfo must be <= 140 characters");
            }

            if (req.RequestedExecutionDate == default)
            {
                errors.Add("requestedExecutionDate is required and must be a valid date");
            }

            if (string.IsNullOrWhiteSpace(req.EndToEndId) || req.EndToEndId.Length > 35)
            {
                errors.Add("endToEndId is required and must be <= 35 characters");
            }

            return errors;
        }

        // Basic IBAN validation algorithm
        public static bool IsValidIban(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return false;
            iban = iban.Replace(" ", string.Empty).ToUpperInvariant();
            if (iban.Length < 15 || iban.Length > 34) return false;
            var rearranged = iban.Substring(4) + iban.Substring(0, 4);
            var converted = ConvertToNumeric(rearranged);
            try
            {
                // compute mod 97
                var remainder = Mod97(converted);
                return remainder == 1;
            }
            catch
            {
                return false;
            }
        }

        private static string ConvertToNumeric(string input)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var ch in input)
            {
                if (char.IsLetter(ch))
                {
                    sb.Append((ch - 'A' + 10).ToString());
                }
                else if (char.IsDigit(ch))
                {
                    sb.Append(ch);
                }
                else
                {
                    throw new ArgumentException("Invalid character in IBAN");
                }
            }
            return sb.ToString();
        }

        private static int Mod97(string input)
        {
            int remainder = 0;
            for (int i = 0; i < input.Length; i += 7)
            {
                var len = Math.Min(7, input.Length - i);
                var part = input.Substring(i, len);
                var value = int.Parse(remainder.ToString() + part);
                remainder = value % 97;
            }
            return remainder;
        }
    }
}
