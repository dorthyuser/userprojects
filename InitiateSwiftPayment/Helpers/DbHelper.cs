using System;
using System.Threading.Tasks;
using Npgsql;
using InitiateSwiftPayment.Models;
using System.Collections.Generic;
using System.Data;

namespace InitiateSwiftPayment.Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;

        public DbHelper(string connectionString)
        {
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            // Map enums
            builder.MapEnum<BankSwiftPaymentTypeEnum>("bank_swift_payment_type_enum");
            builder.MapEnum<BankPaymentStatusEnum>("bank_payment_status_enum");

            _dataSource = builder.Build();
        }

        public async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var conn = await _dataSource.OpenConnectionAsync();
            return conn;
        }

        public async Task<PaymentRecord> InsertPaymentAsync(PaymentRecord record)
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO bank_swift_payments
(payment_type, payment_amount, payment_currency, debtor_account_iban, debtor_bic, creditor_account_iban, creditor_bic, creditor_name, remittance_info, requested_execution_date, end_to_end_id, swift_msg_ref, status, submitted_at, idempotency_key)
VALUES
(@paymentType::bank_swift_payment_type_enum, @paymentAmount, @paymentCurrency, @debtorIban, @debtorBic, @creditorIban, @creditorBic, @creditorName, @remittanceInfo, @requestedExecutionDate, @endToEndId, @swiftMsgRef, @status::bank_payment_status_enum, @submittedAt, @idempotencyKey)
RETURNING id, swift_msg_ref, status, submitted_at;";

            cmd.Parameters.AddWithValue("paymentType", record.PaymentType.ToString());
            cmd.Parameters.AddWithValue("paymentAmount", record.PaymentAmount);
            cmd.Parameters.AddWithValue("paymentCurrency", record.PaymentCurrency);
            cmd.Parameters.AddWithValue("debtorIban", record.DebtorAccountIBAN);
            cmd.Parameters.AddWithValue("debtorBic", record.DebtorBIC);
            cmd.Parameters.AddWithValue("creditorIban", record.CreditorAccountIBAN);
            cmd.Parameters.AddWithValue("creditorBic", record.CreditorBIC);
            cmd.Parameters.AddWithValue("creditorName", record.CreditorName);
            cmd.Parameters.AddWithValue("remittanceInfo", (object?)record.RemittanceInfo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("requestedExecutionDate", record.RequestedExecutionDate);
            cmd.Parameters.AddWithValue("endToEndId", record.EndToEndId);
            cmd.Parameters.AddWithValue("swiftMsgRef", (object?)record.SwiftMsgRef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("status", record.Status.ToString());
            cmd.Parameters.AddWithValue("submittedAt", record.SubmittedAt);
            cmd.Parameters.AddWithValue("idempotencyKey", record.IdempotencyKey);

            try
            {
                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    record.Id = reader.GetInt32(0);
                    record.SwiftMsgRef = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    var statusStr = reader.GetString(2);
                    if (Enum.TryParse<BankPaymentStatusEnum>(statusStr, out var st))
                    {
                        record.Status = st;
                    }
                    record.SubmittedAt = reader.GetFieldValue<DateTime>(3);
                    return record;
                }

                throw new Exception("Failed to insert payment record");
            }
            catch (PostgresException px) when (px.SqlState == "23505")
            {
                // unique violation - likely idempotency
                throw new Exception("Duplicate idempotency key or unique constraint violation: " + px.MessageText, px);
            }
        }

        public async Task<bool> EndToEndExistsAsync(string debtorIban, string endToEndId)
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM bank_swift_payments WHERE debtor_account_iban = @debtorIban AND end_to_end_id = @endToEndId LIMIT 1";
            cmd.Parameters.AddWithValue("debtorIban", debtorIban);
            cmd.Parameters.AddWithValue("endToEndId", endToEndId);
            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync();
        }
    }
}
