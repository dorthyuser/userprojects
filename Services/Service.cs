using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Npgsql;
using Paymentcsharp441Lambda.Models;

namespace Paymentcsharp441Lambda.Services;

public sealed class PaymentServiceException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public object? Extra { get; }
    public PaymentServiceException(int statusCode, string code, string message, object? extra = null) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Extra = extra;
    }
}

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;
    public Service(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<InitiatePaymentResponse> InitiatePaymentAsync(string? body, string requestId, CancellationToken cancellationToken)
    {
        var request = Deserialize<InitiatePaymentRequest>(body);
        ValidateInitiate(request);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT plan_id, amount, currency, plan_name, billing_cycle FROM payment_plans WHERE plan_id = @plan_id AND status = 'ACTIVE'", conn);
        cmd.Parameters.AddWithValue("plan_id", request.PlanId!);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new PaymentServiceException(400, "PLAN_NOT_FOUND", "Plan not found.");
        var planAmount = reader.GetDecimal(1);
        var planCurrency = reader.GetString(2);
        var planName = reader.GetString(3);
        var billingCycle = reader.GetString(4);
        if (!string.Equals(request.Currency, planCurrency, StringComparison.OrdinalIgnoreCase)) throw new PaymentServiceException(400, "INVALID_CURRENCY", "Currency mismatch.");
        var amount = request.Amount.HasValue && request.Amount.Value != planAmount ? planAmount : planAmount;
        var paymentId = $"PAY-{DateTime.UtcNow:yyyy}-{await NextSeqAsync(conn, "pay_id_seq", cancellationToken):D6}";
        var gatewayOrderId = $"order_{Guid.NewGuid().ToString("N")[..12]}";
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var insert = new NpgsqlCommand("INSERT INTO payments (payment_id,user_id,plan_id,amount,currency,payment_method,gateway_name,gateway_order_id,status,description,metadata,email,initiated_at,created_at,updated_at) VALUES (@payment_id,@user_id,@plan_id,@amount,@currency,@payment_method,'RAZORPAY',@gateway_order_id,'PENDING',@description,@metadata::jsonb,@email,NOW(),NOW(),NOW())", conn, tx);
            insert.Parameters.AddWithValue("payment_id", paymentId);
            insert.Parameters.AddWithValue("user_id", request.UserId!);
            insert.Parameters.AddWithValue("plan_id", request.PlanId!);
            insert.Parameters.AddWithValue("amount", amount);
            insert.Parameters.AddWithValue("currency", request.Currency!);
            insert.Parameters.AddWithValue("payment_method", request.PaymentMethod!);
            insert.Parameters.AddWithValue("gateway_order_id", gatewayOrderId);
            insert.Parameters.AddWithValue("description", (object?)request.Description ?? DBNull.Value);
            insert.Parameters.AddWithValue("metadata", (object?)JsonSerializer.Serialize(request.Metadata ?? new Dictionary<string, object>()) ?? DBNull.Value);
            insert.Parameters.AddWithValue("email", request.Email!);
            await insert.ExecuteNonQueryAsync(cancellationToken);
            await using var audit = new NpgsqlCommand("INSERT INTO payment_audit_log (payment_id,action,performed_by,old_status,new_status,notes) VALUES (@payment_id,'INITIATED',@performed_by,NULL,'PENDING',@notes)", conn, tx);
            audit.Parameters.AddWithValue("payment_id", paymentId);
            audit.Parameters.AddWithValue("performed_by", request.UserId!);
            audit.Parameters.AddWithValue("notes", request.Amount.HasValue && request.Amount.Value != planAmount ? "AMOUNT_MISMATCH" : "INITIATED");
            await audit.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return new InitiatePaymentResponse { Status = "success", PaymentId = paymentId, GatewayOrderId = gatewayOrderId, Amount = amount, Currency = request.Currency!, Message = "Payment order created. Complete payment via gateway.", InitiatedAt = DateTime.UtcNow };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw new PaymentServiceException(500, "DB_ERROR", "Database write failure.");
        }
    }

    public async Task<VerifyPaymentResponse> VerifyPaymentAsync(string? body, string requestId, CancellationToken cancellationToken)
    {
        var request = Deserialize<VerifyPaymentRequest>(body);
        ValidateVerify(request);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var fetch = new NpgsqlCommand("SELECT payment_id,gateway_order_id,status,amount,user_id,plan_id,currency FROM payments WHERE payment_id = @payment_id", conn);
        fetch.Parameters.AddWithValue("payment_id", request.PaymentId!);
        await using var reader = await fetch.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new PaymentServiceException(400, "PAYMENT_NOT_FOUND", "Payment not found.");
        var storedOrderId = reader.GetString(1);
        var paymentStatus = reader.GetString(2);
        var amount = reader.GetDecimal(3);
        var userId = reader.GetString(4);
        var planId = reader.GetString(5);
        var currency = reader.GetString(6);
        if (!string.Equals(paymentStatus, "PENDING", StringComparison.OrdinalIgnoreCase)) throw new PaymentServiceException(400, "PAYMENT_NOT_PENDING", "Payment not pending.");
        if (!string.Equals(storedOrderId, request.GatewayOrderId, StringComparison.OrdinalIgnoreCase)) throw new PaymentServiceException(400, "ORDER_ID_MISMATCH", "Order mismatch.");
        var secret = SecretsHelper.Get("gateway_secret", "gateway_secret");
        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{request.GatewayOrderId}|{request.GatewayPaymentId}"))).ToLowerInvariant();
        if (!string.Equals(expected, request.GatewaySignature, StringComparison.OrdinalIgnoreCase)) throw new PaymentServiceException(422, "SIGNATURE_MISMATCH", "Signature mismatch.");
        if (request.Status is "FAILED" or "CANCELLED") throw new PaymentServiceException(422, "GATEWAY_PAYMENT_FAILED", "Gateway payment failed.");
        var verificationId = $"VRF-{DateTime.UtcNow:yyyy}-{await NextSeqAsync(conn, "verify_id_seq", cancellationToken):D6}";
        var invoiceId = $"INV-{DateTime.UtcNow:yyyy}-{await NextSeqAsync(conn, "invoice_id_seq", cancellationToken):D6}";
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var update = new NpgsqlCommand("UPDATE payments SET status='SUCCESS', gateway_payment_id=@gateway_payment_id, completed_at=NOW(), updated_at=NOW() WHERE payment_id=@payment_id", conn, tx);
            update.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
            update.Parameters.AddWithValue("payment_id", request.PaymentId!);
            await update.ExecuteNonQueryAsync(cancellationToken);
            await using var ver = new NpgsqlCommand("INSERT INTO payment_verifications (verification_id,payment_id,gateway_payment_id,gateway_order_id,gateway_signature,verification_source,verification_status,raw_gateway_response,verified_at,created_at) VALUES (@verification_id,@payment_id,@gateway_payment_id,@gateway_order_id,@gateway_signature,@verification_source,'VERIFIED',@raw_gateway_response::jsonb,NOW(),NOW())", conn, tx);
            ver.Parameters.AddWithValue("verification_id", verificationId);
            ver.Parameters.AddWithValue("payment_id", request.PaymentId!);
            ver.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
            ver.Parameters.AddWithValue("gateway_order_id", request.GatewayOrderId!);
            ver.Parameters.AddWithValue("gateway_signature", request.GatewaySignature!);
            ver.Parameters.AddWithValue("verification_source", request.VerificationSource!);
            ver.Parameters.AddWithValue("raw_gateway_response", JsonSerializer.Serialize(new { request.Status }));
            await ver.ExecuteNonQueryAsync(cancellationToken);
            var taxRate = decimal.TryParse(Environment.GetEnvironmentVariable("TAX_RATE_PERCENT"), out var rate) ? rate : 18m;
            var taxAmount = Math.Round(amount * taxRate / 100m, 2);
            var totalAmount = amount + taxAmount;
            await using var inv = new NpgsqlCommand("INSERT INTO invoices (invoice_id,payment_id,user_id,plan_name,billing_cycle,amount,tax_amount,total_amount,currency,invoice_date,status,created_at,updated_at) SELECT @invoice_id,@payment_id,@user_id,p.plan_name,p.billing_cycle,@amount,@tax_amount,@total_amount,@currency,NOW(),'GENERATED',NOW(),NOW() FROM payment_plans p WHERE p.plan_id=@plan_id", conn, tx);
            inv.Parameters.AddWithValue("invoice_id", invoiceId);
            inv.Parameters.AddWithValue("payment_id", request.PaymentId!);
            inv.Parameters.AddWithValue("user_id", userId);
            inv.Parameters.AddWithValue("plan_id", planId);
            inv.Parameters.AddWithValue("amount", amount);
            inv.Parameters.AddWithValue("tax_amount", taxAmount);
            inv.Parameters.AddWithValue("total_amount", totalAmount);
            inv.Parameters.AddWithValue("currency", currency);
            await inv.ExecuteNonQueryAsync(cancellationToken);
            await using var audit = new NpgsqlCommand("INSERT INTO payment_audit_log (payment_id,action,performed_by,old_status,new_status,notes) VALUES (@payment_id,'VERIFIED','system','PENDING','SUCCESS','VERIFIED')", conn, tx);
            audit.Parameters.AddWithValue("payment_id", request.PaymentId!);
            await audit.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return new VerifyPaymentResponse { Status = "success", VerificationId = verificationId, PaymentId = request.PaymentId!, InvoiceId = invoiceId, PaymentStatus = "SUCCESS", SubscriptionActivated = true, Message = "Payment verified. Invoice generated. Subscription activated.", VerifiedAt = DateTime.UtcNow };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw new PaymentServiceException(500, "DB_ERROR", "Database write failure.");
        }
    }

    public async Task<PaymentListResponse> GetPaymentsAsync(IDictionary<string, string>? queryParams, string requestId, CancellationToken cancellationToken)
    {
        var page = queryParams != null && queryParams.TryGetValue("page", out var p) && int.TryParse(p, out var pageVal) && pageVal > 0 ? pageVal : 1;
        var pageSize = queryParams != null && queryParams.TryGetValue("pageSize", out var ps) && int.TryParse(ps, out var psVal) ? psVal : 20;
        if (pageSize > 100) throw new PaymentServiceException(400, "INVALID_PAGE_SIZE", "Page size exceeds maximum.");
        var where = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        AddFilter(queryParams, "userId", "user_id", where, parameters);
        AddFilter(queryParams, "planId", "plan_id", where, parameters);
        AddFilter(queryParams, "status", "status", where, parameters);
        AddFilter(queryParams, "paymentMethod", "payment_method", where, parameters);
        AddFilter(queryParams, "currency", "currency", where, parameters);
        var sqlWhere = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty;
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM payments{sqlWhere}", conn);
        foreach (var param in parameters) countCmd.Parameters.Add(param);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
        await using var cmd = new NpgsqlCommand($"SELECT payment_id,user_id,plan_id,amount,currency,payment_method,status,gateway_order_id,gateway_payment_id,initiated_at,completed_at FROM payments{sqlWhere} ORDER BY initiated_at DESC LIMIT @limit OFFSET @offset", conn);
        foreach (var param in parameters) cmd.Parameters.Add(param);
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        var payments = new List<PaymentItemResponse>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) payments.Add(new PaymentItemResponse { PaymentId = reader.GetString(0), UserId = reader.GetString(1), PlanId = reader.GetString(2), Amount = reader.GetDecimal(3), Currency = reader.GetString(4), PaymentMethod = reader.GetString(5), Status = reader.GetString(6), GatewayOrderId = reader.GetString(7), GatewayPaymentId = reader.IsDBNull(8) ? null : reader.GetString(8), InitiatedAt = reader.GetDateTime(9), CompletedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10) });
        return new PaymentListResponse { Status = "success", Total = total, Page = page, PageSize = pageSize, Payments = payments };
    }

    private static void AddFilter(IDictionary<string, string>? queryParams, string queryKey, string column, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams != null && queryParams.TryGetValue(queryKey, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            where.Add($"{column} = @{queryKey}");
            parameters.Add(new NpgsqlParameter(queryKey, value));
        }
    }

    private static T Deserialize<T>(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) throw new PaymentServiceException(400, "MISSING_REQUIRED_FIELD", "Request body missing.");
        return JsonSerializer.Deserialize<T>(body) ?? throw new PaymentServiceException(400, "MISSING_REQUIRED_FIELD", "Malformed request body.");
    }

    private static void ValidateInitiate(InitiatePaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.PlanId) || !request.Amount.HasValue || string.IsNullOrWhiteSpace(request.Currency) || string.IsNullOrWhiteSpace(request.PaymentMethod) || string.IsNullOrWhiteSpace(request.Email)) throw new PaymentServiceException(400, "MISSING_REQUIRED_FIELD", "Missing required field.");
    }

    private static void ValidateVerify(VerifyPaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentId) || string.IsNullOrWhiteSpace(request.GatewayPaymentId) || string.IsNullOrWhiteSpace(request.GatewayOrderId) || string.IsNullOrWhiteSpace(request.GatewaySignature) || string.IsNullOrWhiteSpace(request.VerificationSource) || string.IsNullOrWhiteSpace(request.Status)) throw new PaymentServiceException(400, "MISSING_REQUIRED_FIELD", "Missing required field.");
    }

    private static async Task<long> NextSeqAsync(NpgsqlConnection conn, string seqName, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand($"SELECT nextval('{seqName}')", conn);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
    }
}
