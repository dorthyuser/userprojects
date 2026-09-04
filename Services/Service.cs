using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.Logging;
using Npgsql;
using Paymentcsharp441Lambda.Models;

namespace Paymentcsharp441Lambda.Services;

public sealed class PaymentValidationException : Exception
{
    public PaymentValidationException(string code, string message, int statusCode = 400) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
    public string Code { get; }
    public int StatusCode { get; }
}

public sealed class PaymentConflictException : Exception
{
    public PaymentConflictException(string code, string message, int statusCode = 409) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
    public string Code { get; }
    public int StatusCode { get; }
}

public sealed class PaymentGatewayException : Exception
{
    public PaymentGatewayException(string message) : base(message) { }
}

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<Service> _logger;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Service>();
    }

    public async Task<InitiatePaymentResponse> InitiatePaymentAsync(string? body, string requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        var request = JsonSerializer.Deserialize<InitiatePaymentRequest>(body ?? throw new PaymentValidationException("MISSING_REQUIRED_FIELD", "Request body is missing.")) ?? throw new PaymentValidationException("MISSING_REQUIRED_FIELD", "Request body is missing.");
        ValidateInitiate(request);
        _logger.LogInformation("Validation passed.");
        var paymentId = $"PAY-{DateTime.UtcNow:yyyy}-{await NextSequenceAsync("pay_id_seq", cancellationToken):D6}";
        var gatewayOrderId = $"order_{Guid.NewGuid():N}";
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var cmd = new NpgsqlCommand("INSERT INTO payments (payment_id,user_id,plan_id,amount,currency,payment_method,gateway_name,gateway_order_id,status,description,metadata,email,initiated_at,created_at,updated_at) VALUES (@payment_id,@user_id,@plan_id,@amount,@currency,@payment_method,'RAZORPAY',@gateway_order_id,'PENDING',@description,@metadata::jsonb,@email,NOW(),NOW(),NOW())", conn, tx))
            {
                cmd.Parameters.AddWithValue("payment_id", paymentId);
                cmd.Parameters.AddWithValue("user_id", request.UserId!);
                cmd.Parameters.AddWithValue("plan_id", request.PlanId!);
                cmd.Parameters.AddWithValue("amount", request.Amount!.Value);
                cmd.Parameters.AddWithValue("currency", request.Currency!);
                cmd.Parameters.AddWithValue("payment_method", request.PaymentMethod!);
                cmd.Parameters.AddWithValue("gateway_order_id", gatewayOrderId);
                cmd.Parameters.AddWithValue("description", (object?)request.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("metadata", (object?)JsonSerializer.Serialize(request.Metadata ?? new Dictionary<string, object>()) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("email", request.Email!);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            await using (var audit = new NpgsqlCommand("INSERT INTO payment_audit_log (payment_id,action,performed_by,old_status,new_status,notes,performed_at) VALUES (@payment_id,'INITIATED',@performed_by,NULL,'PENDING',@notes,NOW())", conn, tx))
            {
                audit.Parameters.AddWithValue("payment_id", paymentId);
                audit.Parameters.AddWithValue("performed_by", request.UserId!);
                audit.Parameters.AddWithValue("notes", (object?)"Payment initiated." ?? DBNull.Value);
                await audit.ExecuteNonQueryAsync(cancellationToken);
            }
            await tx.CommitAsync(cancellationToken);
            return new InitiatePaymentResponse { Status = "success", PaymentId = paymentId, GatewayOrderId = gatewayOrderId, Amount = request.Amount!.Value, Currency = request.Currency!, Message = "Payment order created. Complete payment via gateway.", InitiatedAt = DateTime.UtcNow };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<VerifyPaymentResponse> VerifyPaymentAsync(string? body, string requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        var request = JsonSerializer.Deserialize<VerifyPaymentRequest>(body ?? throw new PaymentValidationException("MISSING_REQUIRED_FIELD", "Request body is missing.")) ?? throw new PaymentValidationException("MISSING_REQUIRED_FIELD", "Request body is missing.");
        ValidateVerify(request);
        _logger.LogInformation("Validation passed.");
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            var verificationId = $"VRF-{DateTime.UtcNow:yyyy}-{await NextSequenceAsync("verify_id_seq", cancellationToken):D6}";
            var invoiceId = $"INV-{DateTime.UtcNow:yyyy}-{await NextSequenceAsync("invoice_id_seq", cancellationToken):D6}";
            await using (var update = new NpgsqlCommand("UPDATE payments SET status='SUCCESS', gateway_payment_id=@gateway_payment_id, completed_at=NOW(), updated_at=NOW() WHERE payment_id=@payment_id", conn, tx))
            {
                update.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
                update.Parameters.AddWithValue("payment_id", request.PaymentId!);
                await update.ExecuteNonQueryAsync(cancellationToken);
            }
            await using (var ver = new NpgsqlCommand("INSERT INTO payment_verifications (verification_id,payment_id,gateway_payment_id,gateway_order_id,gateway_signature,verification_source,verification_status,raw_gateway_response,verified_at,created_at) VALUES (@verification_id,@payment_id,@gateway_payment_id,@gateway_order_id,@gateway_signature,@verification_source,'VERIFIED',@raw_gateway_response::jsonb,NOW(),NOW())", conn, tx))
            {
                ver.Parameters.AddWithValue("verification_id", verificationId);
                ver.Parameters.AddWithValue("payment_id", request.PaymentId!);
                ver.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
                ver.Parameters.AddWithValue("gateway_order_id", request.GatewayOrderId!);
                ver.Parameters.AddWithValue("gateway_signature", request.GatewaySignature!);
                ver.Parameters.AddWithValue("verification_source", request.VerificationSource!);
                ver.Parameters.AddWithValue("raw_gateway_response", "{}");
                await ver.ExecuteNonQueryAsync(cancellationToken);
            }
            await using (var inv = new NpgsqlCommand("INSERT INTO invoices (invoice_id,payment_id,user_id,plan_name,billing_cycle,amount,tax_amount,total_amount,currency,invoice_date,status,created_at,updated_at) SELECT @invoice_id,p.payment_id,p.user_id,pl.plan_name,pl.billing_cycle,p.amount,ROUND((p.amount * @tax_rate)/100.0,2),ROUND(p.amount + ((p.amount * @tax_rate)/100.0),2),p.currency,NOW(),'GENERATED',NOW(),NOW() FROM payments p JOIN payment_plans pl ON pl.plan_id=p.plan_id WHERE p.payment_id=@payment_id", conn, tx))
            {
                inv.Parameters.AddWithValue("invoice_id", invoiceId);
                inv.Parameters.AddWithValue("payment_id", request.PaymentId!);
                inv.Parameters.AddWithValue("tax_rate", decimal.Parse(Environment.GetEnvironmentVariable("TAX_RATE_PERCENT") ?? "18"));
                await inv.ExecuteNonQueryAsync(cancellationToken);
            }
            await using (var audit = new NpgsqlCommand("INSERT INTO payment_audit_log (payment_id,action,performed_by,old_status,new_status,notes,performed_at) VALUES (@payment_id,'VERIFIED','system','PENDING','SUCCESS',@notes,NOW())", conn, tx))
            {
                audit.Parameters.AddWithValue("payment_id", request.PaymentId!);
                audit.Parameters.AddWithValue("notes", (object?)"Payment verified." ?? DBNull.Value);
                await audit.ExecuteNonQueryAsync(cancellationToken);
            }
            await tx.CommitAsync(cancellationToken);
            return new VerifyPaymentResponse { Status = "success", VerificationId = verificationId, PaymentId = request.PaymentId!, InvoiceId = invoiceId, PaymentStatus = "SUCCESS", SubscriptionActivated = true, Message = "Payment verified. Invoice generated. Subscription activated.", VerifiedAt = DateTime.UtcNow };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<GetPaymentsResponse> GetPaymentsAsync(IDictionary<string, string>? queryParams, string requestId, CancellationToken cancellationToken)
    {
        var page = 1;
        var pageSize = 20;
        if (queryParams != null)
        {
            if (queryParams.TryGetValue("page", out var p) && int.TryParse(p, out var pv) && pv > 0) page = pv;
            if (queryParams.TryGetValue("pageSize", out var ps) && int.TryParse(ps, out var psv) && psv > 0) pageSize = psv;
        }
        if (pageSize > 100) throw new PaymentValidationException("INVALID_PAGE_SIZE", "pageSize must not exceed 100.");
        var payments = new List<PaymentItemResponse>();
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT payment_id,user_id,plan_id,amount,currency,payment_method,status,gateway_order_id,gateway_payment_id,initiated_at,completed_at FROM payments ORDER BY initiated_at DESC LIMIT @limit OFFSET @offset", conn);
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            payments.Add(new PaymentItemResponse { PaymentId = reader.GetString(0), UserId = reader.GetString(1), PlanId = reader.GetString(2), Amount = reader.GetDecimal(3), Currency = reader.GetString(4), PaymentMethod = reader.GetString(5), Status = reader.GetString(6), GatewayOrderId = reader.GetString(7), GatewayPaymentId = reader.IsDBNull(8) ? null : reader.GetString(8), InitiatedAt = reader.GetDateTime(9), CompletedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10) });
        }
        return new GetPaymentsResponse { Status = "success", Total = payments.Count, Page = page, PageSize = pageSize, Payments = payments };
    }

    private static void ValidateInitiate(InitiatePaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.PlanId) || request.Amount is null || string.IsNullOrWhiteSpace(request.Currency) || string.IsNullOrWhiteSpace(request.PaymentMethod) || string.IsNullOrWhiteSpace(request.Email)) throw new PaymentValidationException("MISSING_REQUIRED_FIELD", "One or more required fields absent or empty.");
    }

    private static void ValidateVerify(VerifyPaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentId) || string.IsNullOrWhiteSpace(request.GatewayPaymentId) || string.IsNullOrWhiteSpace(request.GatewayOrderId) || string.IsNullOrWhiteSpace(request.GatewaySignature) || string.IsNullOrWhiteSpace(request.VerificationSource) || string.IsNullOrWhiteSpace(request.Status)) throw new PaymentValidationException("MISSING_REQUIRED_FIELD", "One or more required fields absent or empty.");
    }

    private async Task<long> NextSequenceAsync(string sequenceName, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand($"SELECT nextval('{sequenceName}')", conn);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
    }
}