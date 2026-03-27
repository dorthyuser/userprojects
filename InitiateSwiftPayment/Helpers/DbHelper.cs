using System;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Configuration;
using InitiateSwiftPayment.Models;

namespace InitiateSwiftPayment.Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;

        public DbHelper(IConfiguration configuration)
        {
            var connectionString = configuration["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=postgres;Database=bankdb";
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            // Map enums to PostgreSQL enum types
            builder.MapEnum<BankSwiftPaymentTypeEnum>("bank_swift_payment_type_enum");
            builder.MapEnum<BankPaymentStatusEnum>("bank_payment_status_enum");
            _dataSource = builder.Build();
        }

        public async Task<bool> CheckBicExistsAsync(string bic)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM bic_directory WHERE bic = @bic LIMIT 1";
            cmd.Parameters.AddWithValue("bic", bic);
            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }

        public async Task<bool> IsEndToEndUniqueAsync(string debtorIban, string endToEndId)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM bank_swift_payments WHERE debtor_account_iban = @debtor AND end_to_end_id = @e2e LIMIT 1";
            cmd.Parameters.AddWithValue("debtor", debtorIban);
            cmd.Parameters.AddWithValue("e2e", endToEndId);
            var result = await cmd.ExecuteScalarAsync();
            return result == null;
        }

        public async Task<bool> CheckSufficientFundsAsync(string debtorIban, decimal amount, string currency)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            // This assumes an accounts table with balance and currency columns
            cmd.CommandText = "SELECT balance FROM accounts WHERE iban = @iban AND currency = @currency LIMIT 1";
            cmd.Parameters.AddWithValue("iban", debtorIban);
            cmd.Parameters.AddWithValue("currency", currency);
            var result = await cmd.ExecuteScalarAsync();
            if (result == null) return false;
            var balance = Convert.ToDecimal(result);
            return balance >= amount;
        }

        public async Task<bool> IsCurrencySupportedForCorridorAsync(string debtorBic, string creditorBic, string currency)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            // Example table corridor_supported(debtor_bic, creditor_bic, currency)
            cmd.CommandText = "SELECT 1 FROM corridor_supported WHERE debtor_bic = @db AND creditor_bic = @cb AND currency = @cur LIMIT 1";
            cmd.Parameters.AddWithValue("db", debtorBic);
            cmd.Parameters.AddWithValue("cb", creditorBic);
            cmd.Parameters.AddWithValue("cur", currency);
            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }

        public async Task<bool> ExistsByIdempotencyKeyAsync(Guid idempotencyKey)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM bank_swift_payments WHERE idempotency_key = @key LIMIT 1";
            cmd.Parameters.AddWithValue("key", idempotencyKey);
            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }

        public async Task<InsertResult> InsertPaymentAsync(PaymentRequest req, Guid idempotencyKey, string swiftMsgRef)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
INSERT INTO bank_swift_payments
(payment_type, payment_amount, payment_currency, debtor_account_iban, debtor_bic, creditor_account_iban, creditor_bic, creditor_name, remittance_info, requested_execution_date, end_to_end_id, swift_msg_ref, status, idempotency_key)
VALUES
(@paymentType::bank_swift_payment_type_enum, @paymentAmount, @paymentCurrency, @debtorIban, @debtorBic, @creditorIban, @creditorBic, @creditorName, @remittanceInfo, @requestedExecDate, @endToEndId, @swiftMsgRef, @status::bank_payment_status_enum, @idempotencyKey)
RETURNING id, swift_msg_ref, status, submitted_at;
";
                cmd.Parameters.AddWithValue("paymentType", req.PaymentType.ToString());
                cmd.Parameters.AddWithValue("paymentAmount", req.PaymentAmount);
                cmd.Parameters.AddWithValue("paymentCurrency", req.PaymentCurrency);
                cmd.Parameters.AddWithValue("debtorIban", req.DebtorAccountIban);
                cmd.Parameters.AddWithValue("debtorBic", req.DebtorBic);
                cmd.Parameters.AddWithValue("creditorIban", req.CreditorAccountIban);
                cmd.Parameters.AddWithValue("creditorBic", req.CreditorBic);
                cmd.Parameters.AddWithValue("creditorName", req.CreditorName);
                cmd.Parameters.AddWithValue("remittanceInfo", (object)req.RemittanceInfo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("requestedExecDate", req.RequestedExecutionDate);
                cmd.Parameters.AddWithValue("endToEndId", req.EndToEndId);
                cmd.Parameters.AddWithValue("swiftMsgRef", swiftMsgRef);
                cmd.Parameters.AddWithValue("status", BankPaymentStatusEnum.ACCEPTED.ToString());
                cmd.Parameters.AddWithValue("idempotencyKey", idempotencyKey);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var status = reader.GetString(reader.GetOrdinal("status"));
                    var submittedAt = reader.GetFieldValue<DateTime>(reader.GetOrdinal("submitted_at"));
                    await tx.CommitAsync();
                    return new InsertResult { Status = status, SubmittedAt = submittedAt };
                }

                await tx.RollbackAsync();
                throw new Exception("Insert failed, no result returned");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }

    public class InsertResult
    {
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }
}
