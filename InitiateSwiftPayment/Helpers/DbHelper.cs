using System;
using System.Threading.Tasks;
using InitiateSwiftPayment.Models;
using Npgsql;

namespace InitiateSwiftPayment.Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;

        public DbHelper(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            var builder = new NpgsqlDataSourceBuilder(connectionString);
            // Register enums exactly as required
            builder.MapEnum<BankSwiftPaymentType>("bank_swift_payment_type_enum");
            builder.MapEnum<BankPaymentStatus>("bank_payment_status_enum");
            _dataSource = builder.Build();
        }

        public async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var conn = await _dataSource.OpenConnectionAsync();
            return conn;
        }

        public async Task<bool> EndToEndExistsAsync(string debtorIban, string endToEndId)
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM bank_swift_payments WHERE debtor_account_iban = @debtor AND end_to_end_id = @e2e LIMIT 1";
            cmd.Parameters.AddWithValue("debtor", debtorIban);
            cmd.Parameters.AddWithValue("e2e", endToEndId);
            await using var reader = await cmd.ExecuteReaderAsync();
            var exists = await reader.ReadAsync();
            return exists;
        }

        public async Task<(bool Found, InitiateSwiftPaymentResponse? Existing)> GetByIdempotencyKeyAsync(Guid idempotencyKey)
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, swift_msg_ref, status, submitted_at FROM bank_swift_payments WHERE idempotency_key = @k LIMIT 1";
            cmd.Parameters.AddWithValue("k", idempotencyKey);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return (false, null);
            var resp = new InitiateSwiftPaymentResponse
            {
                PaymentId = "pay_" + Guid.NewGuid().ToString(),
                SwiftMsgRef = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Status = reader.IsDBNull(2) ? BankPaymentStatus.ACCEPTED : Enum.Parse<BankPaymentStatus>(reader.GetString(2)),
                SubmittedAt = reader.IsDBNull(3) ? DateTime.UtcNow : reader.GetDateTime(3)
            };
            return (true, resp);
        }

        public async Task<InitiateSwiftPaymentResponse> InsertPaymentAsync(InitiateSwiftPaymentRequest dto, Guid idempotencyKey, string swiftMsgRef, BankPaymentStatus status)
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO bank_swift_payments
(payment_type, payment_amount, payment_currency, debtor_account_iban, debtor_bic, creditor_account_iban, creditor_bic, creditor_name, remittance_info, requested_execution_date, end_to_end_id, swift_msg_ref, status, submitted_at, idempotency_key)
VALUES (@paymentType::bank_swift_payment_type_enum, @paymentAmount, @paymentCurrency, @debtorIban, @debtorBic, @creditorIban, @creditorBic, @creditorName, @remittanceInfo, @requestedDate, @endToEndId, @swiftMsgRef, @status::bank_payment_status_enum, @submittedAt, @idempotencyKey)
RETURNING id, swift_msg_ref, status, submitted_at;";

            cmd.Parameters.AddWithValue("paymentType", dto.PaymentType.ToString());
            cmd.Parameters.AddWithValue("paymentAmount", dto.PaymentAmount);
            cmd.Parameters.AddWithValue("paymentCurrency", dto.PaymentCurrency);
            cmd.Parameters.AddWithValue("debtorIban", dto.DebtorAccountIBAN);
            cmd.Parameters.AddWithValue("debtorBic", dto.DebtorBIC);
            cmd.Parameters.AddWithValue("creditorIban", dto.CreditorAccountIBAN);
            cmd.Parameters.AddWithValue("creditorBic", dto.CreditorBIC);
            cmd.Parameters.AddWithValue("creditorName", dto.CreditorName);
            cmd.Parameters.AddWithValue("remittanceInfo", string.IsNullOrEmpty(dto.RemittanceInfo) ? DBNull.Value : (object)dto.RemittanceInfo);
            cmd.Parameters.AddWithValue("requestedDate", dto.RequestedExecutionDate.Date);
            cmd.Parameters.AddWithValue("endToEndId", dto.EndToEndId);
            cmd.Parameters.AddWithValue("swiftMsgRef", swiftMsgRef);
            cmd.Parameters.AddWithValue("status", status.ToString());
            var submittedAt = DateTime.UtcNow;
            cmd.Parameters.AddWithValue("submittedAt", submittedAt);
            cmd.Parameters.AddWithValue("idempotencyKey", idempotencyKey);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var resp = new InitiateSwiftPaymentResponse
                {
                    PaymentId = "pay_" + Guid.NewGuid().ToString(),
                    SwiftMsgRef = reader.IsDBNull(1) ? swiftMsgRef : reader.GetString(1),
                    Status = reader.IsDBNull(2) ? status : Enum.Parse<BankPaymentStatus>(reader.GetString(2)),
                    SubmittedAt = reader.IsDBNull(3) ? submittedAt : reader.GetDateTime(3)
                };
                return resp;
            }

            throw new Exception("Failed to insert payment");
        }
    }
}
