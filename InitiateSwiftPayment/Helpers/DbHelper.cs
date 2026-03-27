using System;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using Npgsql;
using InitiateSwiftPayment.Models;

namespace InitiateSwiftPayment.Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;

        public DbHelper(string connectionString)
        {
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.MapEnum<BankSwiftPaymentTypeEnum>("bank_swift_payment_type_enum");
            builder.MapEnum<BankPaymentStatusEnum>("bank_payment_status_enum");
            _dataSource = builder.Build();
        }

        public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            return conn;
        }

        public async Task<bool> EndToEndIdExistsAsync(string endToEndId, string debtorIban)
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM bank_swift_payments WHERE end_to_end_id = @end_to_end_id AND debtor_account_iban = @debtor_account_iban";
            cmd.Parameters.AddWithValue("end_to_end_id", endToEndId);
            cmd.Parameters.AddWithValue("debtor_account_iban", debtorIban);
            var res = await cmd.ExecuteScalarAsync();
            if (res is long l) return l > 0;
            if (res is int i) return i > 0;
            return Convert.ToInt64(res) > 0;
        }

        public Task<decimal> GetAccountBalanceAsync(string debtorIban)
        {
            return Task.FromResult(1000000m); // mock
        }

        public async Task<(int Id, DateTime SubmittedAt)> InsertPaymentAsync(PaymentRequest request, Guid idempotencyKey, string swiftMsgRef)
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO bank_swift_payments
(payment_type, payment_amount, payment_currency, debtor_account_iban, debtor_bic, creditor_account_iban, creditor_bic, creditor_name, remittance_info, requested_execution_date, end_to_end_id, swift_msg_ref, status, submitted_at, idempotency_key)
VALUES
(@payment_type::bank_swift_payment_type_enum, @payment_amount, @payment_currency, @debtor_account_iban, @debtor_bic, @creditor_account_iban, @creditor_bic, @creditor_name, @remittance_info, @requested_execution_date, @end_to_end_id, @swift_msg_ref, @status::bank_payment_status_enum, @submitted_at, @idempotency_key)
RETURNING id, submitted_at;
";
            cmd.Parameters.AddWithValue("payment_type", request.PaymentType);
            cmd.Parameters.AddWithValue("payment_amount", request.PaymentAmount);
            cmd.Parameters.AddWithValue("payment_currency", request.PaymentCurrency);
            cmd.Parameters.AddWithValue("debtor_account_iban", request.DebtorAccountIBAN);
            cmd.Parameters.AddWithValue("debtor_bic", request.DebtorBIC);
            cmd.Parameters.AddWithValue("creditor_account_iban", request.CreditorAccountIBAN);
            cmd.Parameters.AddWithValue("creditor_bic", request.CreditorBIC);
            cmd.Parameters.AddWithValue("creditor_name", request.CreditorName);
            if (string.IsNullOrEmpty(request.RemittanceInfo))
                cmd.Parameters.AddWithValue("remittance_info", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("remittance_info", request.RemittanceInfo);
            cmd.Parameters.AddWithValue("requested_execution_date", request.RequestedExecutionDate.Date);
            cmd.Parameters.AddWithValue("end_to_end_id", request.EndToEndId);
            cmd.Parameters.AddWithValue("swift_msg_ref", swiftMsgRef);
            cmd.Parameters.AddWithValue("status", BankPaymentStatusEnum.ACCEPTED);
            var submittedAt = DateTime.UtcNow;
            cmd.Parameters.AddWithValue("submitted_at", submittedAt);
            cmd.Parameters.AddWithValue("idempotency_key", idempotencyKey);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var sa = reader.GetFieldValue<DateTime>(1);
                return (id, sa);
            }

            throw new InvalidOperationException("Failed to insert payment");
        }
    }
}
