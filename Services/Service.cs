using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.Logging;
using Npgsql;
using Paymentcsharp441Lambda.Models;
namespace Paymentcsharp441Lambda.Services;
public sealed class ServiceException : Exception
{
    public ServiceException(int statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
    public int StatusCode { get; }
    public string Code { get; }
}
public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<Service> _logger;
    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
        _logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Service>();
    }
    public async Task<InitiatePaymentResponse> CreatePaymentAsync(string? body, string requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        var request = Deserialize<InitiatePaymentRequest>(body);
        ValidateInitiateRequest(request);
        _logger.LogInformation("Validation passed.");
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var planCmd = new NpgsqlCommand("SELECT plan_id, amount, currency, plan_name, billing_cycle FROM payment_plans WHERE plan_id = @plan_id AND status = 'ACTIVE'", conn);
        planCmd.Parameters.AddWithValue("plan_id", request.PlanId!);
        await using var planReader = await planCmd.ExecuteReaderAsync(cancellationToken);
        if (!await planReader.ReadAsync(cancellationToken)) throw new ServiceException(400, "PLAN_NOT_FOUND", "Plan not found.");
        var planAmount = planReader.GetDecimal(1);
        var planCurrency = planReader.GetString(2);
        var planName = planReader.GetString(3);
        var billingCycle = planReader.GetString(4);
        if (!string.Equals(request.Currency, planCurrency, StringComparison.OrdinalIgnoreCase)) throw new ServiceException(400, "INVALID_CURRENCY", "Currency mismatch.");
        var amount = request.Amount!.Value;
        if (amount != planAmount) amount = planAmount;
        await using var dupCmd = new NpgsqlCommand("SELECT payment_id FROM payments WHERE user_id = @user_id AND plan_id = @plan_id AND initiated_at > NOW() - INTERVAL '120 seconds'", conn);
        dupCmd.Parameters.AddWithValue("user_id", request.UserId!);
        dupCmd.Parameters.AddWithValue("plan_id", request.PlanId!);
        var existing = await dupCmd.ExecuteScalarAsync(cancellationToken);
        if (existing is not null) throw new ServiceException(409, "DUPLICATE_ORDER", "Duplicate order.");
        var gatewayOrderId = $"order_{Guid.NewGuid():N}";
        var paymentId = await GeneratePaymentIdAsync(conn, cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var insertPayment = new NpgsqlCommand("INSERT INTO payments (payment_id, user_id, plan_id, amount, currency, payment_method, gateway_name, gateway_order_id, status, description, metadata, email, initiated_at, created_at, updated_at) VALUES (@payment_id, @user_id, @plan_id, @amount, @currency, @payment_method, 'RAZORPAY', @gateway_order_id, 'PENDING', @description, @metadata::jsonb, @email, NOW(), NOW(), NOW())", conn, tx);
            insertPayment.Parameters.AddWithValue("payment_id", paymentId);
            insertPayment.Parameters.AddWithValue("user_id", request.UserId!);
            insertPayment.Parameters.AddWithValue("plan_id", request.PlanId!);
            insertPayment.Parameters.AddWithValue("amount", amount);
            insertPayment.Parameters.AddWithValue("currency", request.Currency!);
            insertPayment.Parameters.AddWithValue("payment_method", request.PaymentMethod!);
            insertPayment.Parameters.AddWithValue("gateway_order_id", gatewayOrderId);
            insertPayment.Parameters.AddWithValue("description", (object?)request.Description ?? DBNull.Value);
            insertPayment.Parameters.AddWithValue("metadata", (object?)JsonSerializer.Serialize(request.Metadata ?? new Dictionary<string, object>()) ?? DBNull.Value);
            insertPayment.Parameters.AddWithValue("email", request.Email!);
            await insertPayment.ExecuteNonQueryAsync(cancellationToken);
            await using var audit = new NpgsqlCommand("INSERT INTO payment_audit_log (payment_id, action, performed_by, old_status, new_status, notes, performed_at) VALUES (@payment_id, 'INITIATED', @performed_by, NULL, 'PENDING', @notes, NOW())", conn, tx);
            audit.Parameters.AddWithValue("payment_id", paymentId);
            audit.Parameters.AddWithValue("performed_by", request.UserId!);
            audit.Parameters.AddWithValue("notes", amount != request.Amount!.Value ? "Amount coerced to plan amount." : "Payment initiated.");
            await audit.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw new ServiceException(500, "DB_ERROR", "Database write failure.");
        }
        return new InitiatePaymentResponse { Status = "success", PaymentId = paymentId, GatewayOrderId = gatewayOrderId, Amount = amount, Currency = request.Currency!, Message = "Payment order created. Complete payment via gateway.", InitiatedAt = DateTime.UtcNow };
    }
    public async Task<VerifyPaymentResponse> VerifyPaymentAsync(string? body, string requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        var request = Deserialize<VerifyPaymentRequest>(body);
        ValidateVerifyRequest(request);
        _logger.LogInformation("Validation passed.");
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var paymentCmd = new NpgsqlCommand("SELECT payment_id, status, gateway_order_id, amount, user_id, plan_id, currency FROM payments WHERE payment_id = @payment_id", conn);
        paymentCmd.Parameters.AddWithValue("payment_id", request.PaymentId!);
        await using var reader = await paymentCmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new ServiceException(400, "PAYMENT_NOT_FOUND", "Payment not found.");
        var paymentStatus = reader.GetString(1);
        var storedGatewayOrderId = reader.GetString(2);
        var amount = reader.GetDecimal(3);
        var userId = reader.GetString(4);
        var planId = reader.GetString(5);
        var currency = reader.GetString(6);
        if (!string.Equals(paymentStatus, "PENDING", StringComparison.OrdinalIgnoreCase)) throw new ServiceException(400, "PAYMENT_NOT_PENDING", "Payment not pending.");
        if (!string.Equals(storedGatewayOrderId, request.GatewayOrderId, StringComparison.OrdinalIgnoreCase)) throw new ServiceException(400, "ORDER_ID_MISMATCH", "Order mismatch.");
        await using var dupVerify = new NpgsqlCommand("SELECT verification_id FROM payment_verifications WHERE payment_id = @payment_id", conn);
        dupVerify.Parameters.AddWithValue("payment_id", request.PaymentId!);
        var existing = await dupVerify.ExecuteScalarAsync(cancellationToken);
        if (existing is not null) throw new ServiceException(409, "ALREADY_VERIFIED", "Already verified.");
        var secret = SecretsHelper.Get("gateway_secret", "gateway_secret");
        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{request.GatewayOrderId}|{request.GatewayPaymentId}"))).ToLowerInvariant();
        if (!string.Equals(expected, request.GatewaySignature, StringComparison.OrdinalIgnoreCase))
        {
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            try
            {
                await using var insertFail = new NpgsqlCommand("INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response, verified_at, created_at) VALUES (@verification_id, @payment_id, @gateway_payment_id, @gateway_order_id, @gateway_signature, @verification_source, 'SIGNATURE_MISMATCH', @raw_gateway_response::jsonb, NOW(), NOW())", conn, tx);
                insertFail.Parameters.AddWithValue("verification_id", await GenerateVerificationIdAsync(conn, cancellationToken));
                insertFail.Parameters.AddWithValue("payment_id", request.PaymentId!);
                insertFail.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
                insertFail.Parameters.AddWithValue("gateway_order_id", request.GatewayOrderId!);
                insertFail.Parameters.AddWithValue("gateway_signature", request.GatewaySignature!);
                insertFail.Parameters.AddWithValue("verification_source", request.VerificationSource!);
                insertFail.Parameters.AddWithValue("raw_gateway_response", JsonSerializer.Serialize(new { status = request.Status, source = request.VerificationSource }));
                await insertFail.ExecuteNonQueryAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw new ServiceException(500, "DB_ERROR", "Database write failure.");
            }
            throw new ServiceException(422, "SIGNATURE_MISMATCH", "Signature mismatch.");
        }
        if (string.Equals(request.Status, "FAILED", StringComparison.OrdinalIgnoreCase) || string.Equals(request.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            try
            {
                await using var updatePayment = new NpgsqlCommand("UPDATE payments SET status = 'FAILED', gateway_payment_id = @gateway_payment_id, completed_at = NOW(), updated_at = NOW() WHERE payment_id = @payment_id", conn, tx);
                updatePayment.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
                updatePayment.Parameters.AddWithValue("payment_id", request.PaymentId!);
                await updatePayment.ExecuteNonQueryAsync(cancellationToken);
                await using var insertFail = new NpgsqlCommand("INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response, verified_at, created_at) VALUES (@verification_id, @payment_id, @gateway_payment_id, @gateway_order_id, @gateway_signature, @verification_source, 'FAILED', @raw_gateway_response::jsonb, NOW(), NOW())", conn, tx);
                insertFail.Parameters.AddWithValue("verification_id", await GenerateVerificationIdAsync(conn, cancellationToken));
                insertFail.Parameters.AddWithValue("payment_id", request.PaymentId!);
                insertFail.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
                insertFail.Parameters.AddWithValue("gateway_order_id", request.GatewayOrderId!);
                insertFail.Parameters.AddWithValue("gateway_signature", request.GatewaySignature!);
                insertFail.Parameters.AddWithValue("verification_source", request.VerificationSource!);
                insertFail.Parameters.AddWithValue("raw_gateway_response", JsonSerializer.Serialize(new { status = request.Status, source = request.VerificationSource }));
                await insertFail.ExecuteNonQueryAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw new ServiceException(500, "DB_ERROR", "Database write failure.");
            }
            throw new ServiceException(422, "GATEWAY_PAYMENT_FAILED", "Gateway payment failed.");
        }
        var verificationId = await GenerateVerificationIdAsync(conn, cancellationToken);
        var invoiceId = await GenerateInvoiceIdAsync(conn, cancellationToken);
        var taxRate = decimal.TryParse(Environment.GetEnvironmentVariable("TAX_RATE_PERCENT"), out var rate) ? rate : 18m;
        var taxAmount = Math.Round(amount * taxRate / 100m, 2);
        var totalAmount = amount + taxAmount;
        await using var tx2 = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var updatePayment = new NpgsqlCommand("UPDATE payments SET status = 'SUCCESS', gateway_payment_id = @gateway_payment_id, completed_at = NOW(), updated_at = NOW() WHERE payment_id = @payment_id", conn, tx2);
            updatePayment.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
            updatePayment.Parameters.AddWithValue("payment_id", request.PaymentId!);
            await updatePayment.ExecuteNonQueryAsync(cancellationToken);
            await using var insertVerification = new NpgsqlCommand("INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response, verified_at, created_at) VALUES (@verification_id, @payment_id, @gateway_payment_id, @gateway_order_id, @gateway_signature, @verification_source, 'VERIFIED', @raw_gateway_response::jsonb, NOW(), NOW())", conn, tx2);
            insertVerification.Parameters.AddWithValue("verification_id", verificationId);
            insertVerification.Parameters.AddWithValue("payment_id", request.PaymentId!);
            insertVerification.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
            insertVerification.Parameters.AddWithValue("gateway_order_id", request.GatewayOrderId!);
            insertVerification.Parameters.AddWithValue("gateway_signature", request.GatewaySignature!);
            insertVerification.Parameters.AddWithValue("verification_source", request.VerificationSource!);
            insertVerification.Parameters.AddWithValue("raw_gateway_response", JsonSerializer.Serialize(new { status = request.Status, source = request.VerificationSource }));
            await insertVerification.ExecuteNonQueryAsync(cancellationToken);
            await using var insertInvoice = new NpgsqlCommand("INSERT INTO invoices (invoice_id, payment_id, user_id, plan_name, billing_cycle, amount, tax_amount, total_amount, currency, invoice_date, status, created_at, updated_at) SELECT @invoice_id, p.payment_id, p.user_id, pl.plan_name, pl.billing_cycle, @amount, @tax_amount, @total_amount, @currency, NOW(), 'GENERATED', NOW(), NOW() FROM payments p JOIN payment_plans pl ON pl.plan_id = p.plan_id WHERE p.payment_id = @payment_id", conn, tx2);
            insertInvoice.Parameters.AddWithValue("invoice_id", invoiceId);
            insertInvoice.Parameters.AddWithValue("payment_id", request.PaymentId!);
            insertInvoice.Parameters.AddWithValue("amount", amount);
            insertInvoice.Parameters.AddWithValue("tax_amount", taxAmount);
            insertInvoice.Parameters.AddWithValue("total_amount", totalAmount);
            insertInvoice.Parameters.AddWithValue("currency", currency);
            await insertInvoice.ExecuteNonQueryAsync(cancellationToken);
            await using var audit = new NpgsqlCommand("INSERT INTO payment_audit_log (payment_id, action, performed_by, old_status, new_status, notes, performed_at) VALUES (@payment_id, 'VERIFIED', 'system', 'PENDING', 'SUCCESS', @notes, NOW())", conn, tx2);
            audit.Parameters.AddWithValue("payment_id", request.PaymentId!);
            audit.Parameters.AddWithValue("notes", "Payment verified and subscription activated.");
            await audit.ExecuteNonQueryAsync(cancellationToken);
            await tx2.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx2.RollbackAsync(cancellationToken);
            throw new ServiceException(500, "DB_ERROR", "Database write failure.");
        }
        return new VerifyPaymentResponse { Status = "success", VerificationId = verificationId, PaymentId = request.PaymentId!, InvoiceId = invoiceId, PaymentStatus = "SUCCESS", SubscriptionActivated = true, Message = "Payment verified. Invoice generated. Subscription activated.", VerifiedAt = DateTime.UtcNow };
    }
    public async Task<GetPaymentsResponse> GetPaymentsAsync(IDictionary<string, string>? queryParams, string requestId, CancellationToken cancellationToken)
    {
        var page = 1;
        var pageSize = 20;
        if (queryParams is not null)
        {
            if (queryParams.TryGetValue("page", out var pageValue) && int.TryParse(pageValue, out var parsedPage) && parsedPage > 0) page = parsedPage;
            if (queryParams.TryGetValue("pageSize", out var pageSizeValue) && int.TryParse(pageSizeValue, out var parsedPageSize) && parsedPageSize > 0) pageSize = parsedPageSize;
        }
        if (pageSize > 100) throw new ServiceException(400, "INVALID_PAGE_SIZE", "Page size too large.");
        var where = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        void AddFilter(string name, string clause, string value)
        {
            where.Add(clause);
            parameters.Add(new NpgsqlParameter(name, value));
        }
        if (queryParams is not null)
        {
            if (queryParams.TryGetValue("userId", out var userId) && !string.IsNullOrWhiteSpace(userId)) AddFilter("user_id", "user_id = @user_id", userId);
            if (queryParams.TryGetValue("planId", out var planId) && !string.IsNullOrWhiteSpace(planId)) AddFilter("plan_id", "plan_id = @plan_id", planId);
            if (queryParams.TryGetValue("status", out var status) && !string.IsNullOrWhiteSpace(status)) AddFilter("status", "status = @status", status);
            if (queryParams.TryGetValue("paymentMethod", out var paymentMethod) && !string.IsNullOrWhiteSpace(paymentMethod)) AddFilter("payment_method", "payment_method = @payment_method", paymentMethod);
            if (queryParams.TryGetValue("currency", out var currency) && !string.IsNullOrWhiteSpace(currency)) AddFilter("currency", "currency = @currency", currency);
            if (queryParams.TryGetValue("dateFrom", out var dateFrom) && !string.IsNullOrWhiteSpace(dateFrom)) AddFilter("date_from", "initiated_at >= @date_from", dateFrom);
            if (queryParams.TryGetValue("dateTo", out var dateTo) && !string.IsNullOrWhiteSpace(dateTo)) AddFilter("date_to", "initiated_at <= @date_to", dateTo);
        }
        var whereClause = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty;
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM payments{whereClause}", conn);
        foreach (var p in parameters) countCmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        var total = Convert.ToInt64(await countCmd.ExecuteScalarAsync(cancellationToken));
        await using var selectCmd = new NpgsqlCommand($"SELECT payment_id, user_id, plan_id, amount, currency, payment_method, status, gateway_order_id, gateway_payment_id, initiated_at, completed_at FROM payments{whereClause} ORDER BY initiated_at DESC LIMIT @limit OFFSET @offset", conn);
        foreach (var p in parameters) selectCmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        selectCmd.Parameters.AddWithValue("limit", pageSize);
        selectCmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        var payments = new List<PaymentRecordResponse>();
        await using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            payments.Add(new PaymentRecordResponse { PaymentId = reader.GetString(0), UserId = reader.GetString(1), PlanId = reader.GetString(2), Amount = reader.GetDecimal(3), Currency = reader.GetString(4), PaymentMethod = reader.GetString(5), Status = reader.GetString(6), GatewayOrderId = reader.GetString(7), GatewayPaymentId = reader.IsDBNull(8) ? null : reader.GetString(8), InitiatedAt = reader.GetDateTime(9), CompletedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10) });
        }
        return new GetPaymentsResponse { Status = "success", Total = total, Page = page, PageSize = pageSize, Payments = payments };
    }
    private static T Deserialize<T>(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) throw new ServiceException(400, "MISSING_REQUIRED_FIELD", "Request body missing.");
        return JsonSerializer.Deserialize<T>(body) ?? throw new ServiceException(400, "MISSING_REQUIRED_FIELD", "Malformed request body.");
    }
    private static void ValidateInitiateRequest(InitiatePaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.PlanId) || request.Amount is null || string.IsNullOrWhiteSpace(request.Currency) || string.IsNullOrWhiteSpace(request.PaymentMethod) || string.IsNullOrWhiteSpace(request.Email)) throw new ServiceException(400, "MISSING_REQUIRED_FIELD", "Missing required field.");
    }
    private static void ValidateVerifyRequest(VerifyPaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentId) || string.IsNullOrWhiteSpace(request.GatewayPaymentId) || string.IsNullOrWhiteSpace(request.GatewayOrderId) || string.IsNullOrWhiteSpace(request.GatewaySignature) || string.IsNullOrWhiteSpace(request.VerificationSource) || string.IsNullOrWhiteSpace(request.Status)) throw new ServiceException(400, "MISSING_REQUIRED_FIELD", "Missing required field.");
    }
    private static async Task<string> GeneratePaymentIdAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("SELECT nextval('pay_id_seq')", conn);
        var seq = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
        return $"PAY-{DateTime.UtcNow:yyyy}-{seq:D6}";
    }
    private static async Task<string> GenerateVerificationIdAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("SELECT nextval('verify_id_seq')", conn);
        var seq = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
        return $"VRF-{DateTime.UtcNow:yyyy}-{seq:D6}";
    }
    private static async Task<string> GenerateInvoiceIdAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("SELECT nextval('invoice_id_seq')", conn);
        var seq = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
        return $"INV-{DateTime.UtcNow:yyyy}-{seq:D6}";
    }
}