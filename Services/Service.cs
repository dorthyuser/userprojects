using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using Paymentcsharp441Lambda.Models;

namespace Paymentcsharp441Lambda.Services;

public sealed class PaymentServiceException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public object? Extra { get; }

    public PaymentServiceException(int statusCode, string code, string message, object? extra = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Extra = extra;
    }
}

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly HttpClient _httpClient;

    private static readonly HashSet<string> ValidPaymentMethods =
        new(StringComparer.OrdinalIgnoreCase) { "UPI", "CARD", "NETBANKING", "WALLET", "BANK_TRANSFER" };

    private static readonly HashSet<string> ValidVerificationSources =
        new(StringComparer.OrdinalIgnoreCase) { "WEBHOOK", "POLLING", "MANUAL" };

    public Service(NpgsqlDataSource dataSource, HttpClient httpClient)
    {
        _dataSource  = dataSource;
        _httpClient  = httpClient;
    }

    // ──────────────────────────────────────────────────────────────
    //  POST /v1/payments  —  Initiate payment
    // ──────────────────────────────────────────────────────────────
    public async Task<InitiatePaymentResponse> InitiatePaymentAsync(
        string? body, string requestId, CancellationToken cancellationToken)
    {
        var request = Deserialize<InitiatePaymentRequest>(body);
        ValidateInitiate(request);                                          // steps 2 & 3

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);

        // Step 4 — verify plan exists; reader is closed inside this block before any
        // further command runs on the same connection (fixes the open-reader bug).
        decimal planAmount;
        string planCurrency;
        await using (var planCmd = new NpgsqlCommand(
            "SELECT plan_id, amount, currency, plan_name, billing_cycle " +
            "FROM payment_plans WHERE plan_id = @plan_id AND status = 'ACTIVE'", conn))
        {
            planCmd.Parameters.AddWithValue("plan_id", request.PlanId!);
            await using var planReader = await planCmd.ExecuteReaderAsync(cancellationToken);
            if (!await planReader.ReadAsync(cancellationToken))
                throw new PaymentServiceException(400, "PLAN_NOT_FOUND", "Plan not found.");
            planAmount   = planReader.GetDecimal(1);
            planCurrency = planReader.GetString(2);
            // planReader & planCmd disposed here — conn is free for the next command
        }

        // Step 6 — currency hard-reject
        if (!string.Equals(request.Currency, planCurrency, StringComparison.OrdinalIgnoreCase))
            throw new PaymentServiceException(400, "INVALID_CURRENCY", "Currency mismatch.");

        // Step 5 — amount coercion (always use plan amount; log mismatch in audit)
        var amount     = planAmount;
        var auditNote  = (request.Amount.HasValue && request.Amount.Value != planAmount)
                         ? "AMOUNT_MISMATCH" : "INITIATED";

        // Step 7 — idempotency check: duplicate within the configured window
        var windowSec = int.TryParse(Environment.GetEnvironmentVariable("IDEMPOTENCY_WINDOW_SEC"), out var iw)
                        ? iw : 120;
        await using (var idempCmd = new NpgsqlCommand(
            "SELECT payment_id FROM payments " +
            "WHERE user_id = @user_id AND plan_id = @plan_id " +
            "AND initiated_at > NOW() - (INTERVAL '1 second' * @window) LIMIT 1", conn))
        {
            idempCmd.Parameters.AddWithValue("user_id", request.UserId!);
            idempCmd.Parameters.AddWithValue("plan_id", request.PlanId!);
            idempCmd.Parameters.AddWithValue("window", windowSec);
            var dup = await idempCmd.ExecuteScalarAsync(cancellationToken);
            if (dup is string dupId)
                throw new PaymentServiceException(409, "DUPLICATE_ORDER",
                    "Duplicate payment order within idempotency window.",
                    new { paymentId = dupId });
        }

        // Step 8 — create gateway order (with retry)
        var gatewayOrderId = await CreateGatewayOrderAsync(amount, request.Currency!, requestId, cancellationToken);

        // Step 9 — generate payment_id from sequence (conn has no open reader now)
        var paymentId = $"PAY-{DateTime.UtcNow:yyyy}-{await NextSeqAsync(conn, "pay_id_seq", cancellationToken):D6}";

        // Steps 10–12 — atomic transaction: INSERT payments + audit
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var insert = new NpgsqlCommand(
                "INSERT INTO payments " +
                "(payment_id,user_id,plan_id,amount,currency,payment_method,gateway_name," +
                "gateway_order_id,status,description,metadata,email,initiated_at,created_at,updated_at) " +
                "VALUES (@payment_id,@user_id,@plan_id,@amount,@currency,@payment_method,'RAZORPAY'," +
                "@gateway_order_id,'PENDING',@description,@metadata::jsonb,@email,NOW(),NOW(),NOW())",
                conn, tx);
            insert.Parameters.AddWithValue("payment_id",      paymentId);
            insert.Parameters.AddWithValue("user_id",         request.UserId!);
            insert.Parameters.AddWithValue("plan_id",         request.PlanId!);
            insert.Parameters.AddWithValue("amount",          amount);
            insert.Parameters.AddWithValue("currency",        request.Currency!);
            insert.Parameters.AddWithValue("payment_method",  request.PaymentMethod!);
            insert.Parameters.AddWithValue("gateway_order_id",gatewayOrderId);
            insert.Parameters.AddWithValue("description",     (object?)request.Description ?? DBNull.Value);
            insert.Parameters.AddWithValue("metadata",        JsonSerializer.Serialize(request.Metadata ?? new Dictionary<string, object>()));
            insert.Parameters.AddWithValue("email",           request.Email!);
            await insert.ExecuteNonQueryAsync(cancellationToken);

            await using var audit = new NpgsqlCommand(
                "INSERT INTO payment_audit_log " +
                "(payment_id,action,performed_by,old_status,new_status,notes) " +
                "VALUES (@payment_id,'INITIATED',@performed_by,NULL,'PENDING',@notes)",
                conn, tx);
            audit.Parameters.AddWithValue("payment_id",   paymentId);
            audit.Parameters.AddWithValue("performed_by", request.UserId!);
            audit.Parameters.AddWithValue("notes",        auditNote);
            await audit.ExecuteNonQueryAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);

            return new InitiatePaymentResponse
            {
                Status        = "success",
                PaymentId     = paymentId,
                GatewayOrderId= gatewayOrderId,
                Amount        = amount,
                Currency      = request.Currency!,
                Message       = "Payment order created. Complete payment via gateway.",
                InitiatedAt   = DateTime.UtcNow
            };
        }
        catch (Exception ex) when (ex is not PaymentServiceException)
        {
            await tx.RollbackAsync(CancellationToken.None);
            Console.Error.WriteLine($"[{requestId}] InitiatePaymentAsync write failure: {ex}");
            throw new PaymentServiceException(500, "DB_ERROR", "Database write failure.");
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  POST /v1/payments/verify  —  The Check
    // ──────────────────────────────────────────────────────────────
    public async Task<VerifyPaymentResponse> VerifyPaymentAsync(
        string? body, string requestId, CancellationToken cancellationToken)
    {
        var request = Deserialize<VerifyPaymentRequest>(body);
        ValidateVerify(request);                                            // steps 2 & 3

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);

        // Step 4 — fetch payment record; reader closed in explicit block
        string storedOrderId, paymentStatus, userId, planId, currency;
        decimal amount;
        await using (var fetchCmd = new NpgsqlCommand(
            "SELECT payment_id,gateway_order_id,status,amount,user_id,plan_id,currency " +
            "FROM payments WHERE payment_id = @payment_id", conn))
        {
            fetchCmd.Parameters.AddWithValue("payment_id", request.PaymentId!);
            await using var fetchReader = await fetchCmd.ExecuteReaderAsync(cancellationToken);
            if (!await fetchReader.ReadAsync(cancellationToken))
                throw new PaymentServiceException(400, "PAYMENT_NOT_FOUND", "Payment not found.");
            storedOrderId  = fetchReader.GetString(1);
            paymentStatus  = fetchReader.GetString(2);
            amount         = fetchReader.GetDecimal(3);
            userId         = fetchReader.GetString(4);
            planId         = fetchReader.GetString(5);
            currency       = fetchReader.GetString(6);
            // fetchReader & fetchCmd disposed here — conn is free
        }

        // Step 5 — status check
        if (!string.Equals(paymentStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
            throw new PaymentServiceException(400, "PAYMENT_NOT_PENDING", "Payment not pending.");

        // Step 6 — order ID check
        if (!string.Equals(storedOrderId, request.GatewayOrderId, StringComparison.OrdinalIgnoreCase))
            throw new PaymentServiceException(400, "ORDER_ID_MISMATCH", "Order mismatch.");

        // Step 7 — idempotency: already verified?
        await using (var alreadyCmd = new NpgsqlCommand(
            "SELECT verification_id FROM payment_verifications WHERE payment_id = @payment_id LIMIT 1", conn))
        {
            alreadyCmd.Parameters.AddWithValue("payment_id", request.PaymentId!);
            var existingVId = await alreadyCmd.ExecuteScalarAsync(cancellationToken);
            if (existingVId is string evId)
                throw new PaymentServiceException(409, "ALREADY_VERIFIED", "Payment already verified.",
                    new { verificationId = evId });
        }

        // Step 8 — HMAC-SHA256 signature check
        var secret   = SecretsHelper.Get("gateway_secret", "gateway_secret");
        var expected = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes($"{request.GatewayOrderId}|{request.GatewayPaymentId}")))
            .ToLowerInvariant();

        if (!string.Equals(expected, request.GatewaySignature, StringComparison.OrdinalIgnoreCase))
        {
            // Spec: persist FAILED verification record; payment stays PENDING
            var failVerifId = $"VRF-{DateTime.UtcNow:yyyy}-{await NextSeqAsync(conn, "verify_id_seq", cancellationToken):D6}";
            await using var failVer = new NpgsqlCommand(
                "INSERT INTO payment_verifications " +
                "(verification_id,payment_id,gateway_payment_id,gateway_order_id,gateway_signature," +
                "verification_source,verification_status,raw_gateway_response,verified_at,created_at) " +
                "VALUES (@verification_id,@payment_id,@gateway_payment_id,@gateway_order_id,@gateway_signature," +
                "@verification_source,'SIGNATURE_MISMATCH',@raw::jsonb,NOW(),NOW())", conn);
            failVer.Parameters.AddWithValue("verification_id",    failVerifId);
            failVer.Parameters.AddWithValue("payment_id",         request.PaymentId!);
            failVer.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
            failVer.Parameters.AddWithValue("gateway_order_id",   request.GatewayOrderId!);
            failVer.Parameters.AddWithValue("gateway_signature",  request.GatewaySignature!);
            failVer.Parameters.AddWithValue("verification_source",request.VerificationSource!);
            failVer.Parameters.AddWithValue("raw",                JsonSerializer.Serialize(new { request.Status, mismatch = true }));
            await failVer.ExecuteNonQueryAsync(cancellationToken);
            throw new PaymentServiceException(422, "SIGNATURE_MISMATCH", "Signature mismatch.");
        }

        // Step 9 — gateway status: FAILED or CANCELLED
        if (request.Status is "FAILED" or "CANCELLED")
        {
            // Spec: update payment to FAILED + persist FAILED verification record
            var failVerifId = $"VRF-{DateTime.UtcNow:yyyy}-{await NextSeqAsync(conn, "verify_id_seq", cancellationToken):D6}";
            await using var failTx = await conn.BeginTransactionAsync(cancellationToken);
            try
            {
                await using var failUpd = new NpgsqlCommand(
                    "UPDATE payments SET status='FAILED', updated_at=NOW() WHERE payment_id=@payment_id",
                    conn, failTx);
                failUpd.Parameters.AddWithValue("payment_id", request.PaymentId!);
                await failUpd.ExecuteNonQueryAsync(cancellationToken);

                await using var failVer = new NpgsqlCommand(
                    "INSERT INTO payment_verifications " +
                    "(verification_id,payment_id,gateway_payment_id,gateway_order_id,gateway_signature," +
                    "verification_source,verification_status,raw_gateway_response,verified_at,created_at) " +
                    "VALUES (@verification_id,@payment_id,@gateway_payment_id,@gateway_order_id,@gateway_signature," +
                    "@verification_source,'FAILED',@raw::jsonb,NOW(),NOW())", conn, failTx);
                failVer.Parameters.AddWithValue("verification_id",    failVerifId);
                failVer.Parameters.AddWithValue("payment_id",         request.PaymentId!);
                failVer.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
                failVer.Parameters.AddWithValue("gateway_order_id",   request.GatewayOrderId!);
                failVer.Parameters.AddWithValue("gateway_signature",  request.GatewaySignature!);
                failVer.Parameters.AddWithValue("verification_source",request.VerificationSource!);
                failVer.Parameters.AddWithValue("raw",                JsonSerializer.Serialize(new { request.Status }));
                await failVer.ExecuteNonQueryAsync(cancellationToken);

                await failTx.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await failTx.RollbackAsync(CancellationToken.None);
                Console.Error.WriteLine($"[{requestId}] GATEWAY_PAYMENT_FAILED write error: {ex}");
            }
            throw new PaymentServiceException(422, "GATEWAY_PAYMENT_FAILED", "Gateway payment failed.");
        }

        // Step 10 — generate identifiers (conn has no open reader or transaction now)
        var verificationId = $"VRF-{DateTime.UtcNow:yyyy}-{await NextSeqAsync(conn, "verify_id_seq", cancellationToken):D6}";
        var invoiceId      = $"INV-{DateTime.UtcNow:yyyy}-{await NextSeqAsync(conn, "invoice_id_seq", cancellationToken):D6}";

        // Steps 11–15 — atomic transaction: 4 writes
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            // Step 12 — UPDATE payments
            await using var update = new NpgsqlCommand(
                "UPDATE payments SET status='SUCCESS', gateway_payment_id=@gateway_payment_id, " +
                "completed_at=NOW(), updated_at=NOW() WHERE payment_id=@payment_id", conn, tx);
            update.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
            update.Parameters.AddWithValue("payment_id",         request.PaymentId!);
            await update.ExecuteNonQueryAsync(cancellationToken);

            // Step 13 — INSERT payment_verifications
            await using var ver = new NpgsqlCommand(
                "INSERT INTO payment_verifications " +
                "(verification_id,payment_id,gateway_payment_id,gateway_order_id,gateway_signature," +
                "verification_source,verification_status,raw_gateway_response,verified_at,created_at) " +
                "VALUES (@verification_id,@payment_id,@gateway_payment_id,@gateway_order_id,@gateway_signature," +
                "@verification_source,'VERIFIED',@raw::jsonb,NOW(),NOW())", conn, tx);
            ver.Parameters.AddWithValue("verification_id",    verificationId);
            ver.Parameters.AddWithValue("payment_id",         request.PaymentId!);
            ver.Parameters.AddWithValue("gateway_payment_id", request.GatewayPaymentId!);
            ver.Parameters.AddWithValue("gateway_order_id",   request.GatewayOrderId!);
            ver.Parameters.AddWithValue("gateway_signature",  request.GatewaySignature!);
            ver.Parameters.AddWithValue("verification_source",request.VerificationSource!);
            ver.Parameters.AddWithValue("raw",                JsonSerializer.Serialize(new { request.Status }));
            await ver.ExecuteNonQueryAsync(cancellationToken);

            // Step 14 — INSERT invoices with tax calculation
            var taxRate    = decimal.TryParse(Environment.GetEnvironmentVariable("TAX_RATE_PERCENT"), out var rate) ? rate : 18m;
            var taxAmount  = Math.Round(amount * taxRate / 100m, 2);
            var totalAmount= amount + taxAmount;
            await using var inv = new NpgsqlCommand(
                "INSERT INTO invoices " +
                "(invoice_id,payment_id,user_id,plan_name,billing_cycle,amount,tax_amount,total_amount," +
                "currency,invoice_date,status,created_at,updated_at) " +
                "SELECT @invoice_id,@payment_id,@user_id,p.plan_name,p.billing_cycle," +
                "@amount,@tax_amount,@total_amount,@currency,NOW(),'GENERATED',NOW(),NOW() " +
                "FROM payment_plans p WHERE p.plan_id=@plan_id", conn, tx);
            inv.Parameters.AddWithValue("invoice_id",   invoiceId);
            inv.Parameters.AddWithValue("payment_id",   request.PaymentId!);
            inv.Parameters.AddWithValue("user_id",      userId);
            inv.Parameters.AddWithValue("plan_id",      planId);
            inv.Parameters.AddWithValue("amount",       amount);
            inv.Parameters.AddWithValue("tax_amount",   taxAmount);
            inv.Parameters.AddWithValue("total_amount", totalAmount);
            inv.Parameters.AddWithValue("currency",     currency);
            await inv.ExecuteNonQueryAsync(cancellationToken);

            // Step 15 — INSERT audit
            await using var audit = new NpgsqlCommand(
                "INSERT INTO payment_audit_log " +
                "(payment_id,action,performed_by,old_status,new_status,notes) " +
                "VALUES (@payment_id,'VERIFIED','system','PENDING','SUCCESS','VERIFIED')", conn, tx);
            audit.Parameters.AddWithValue("payment_id", request.PaymentId!);
            await audit.ExecuteNonQueryAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);

            return new VerifyPaymentResponse
            {
                Status               = "success",
                VerificationId       = verificationId,
                PaymentId            = request.PaymentId!,
                InvoiceId            = invoiceId,
                PaymentStatus        = "SUCCESS",
                SubscriptionActivated= true,
                Message              = "Payment verified. Invoice generated. Subscription activated.",
                VerifiedAt           = DateTime.UtcNow
            };
        }
        catch (Exception ex) when (ex is not PaymentServiceException)
        {
            await tx.RollbackAsync(CancellationToken.None);
            Console.Error.WriteLine($"[{requestId}] VerifyPaymentAsync write failure: {ex}");
            throw new PaymentServiceException(500, "DB_ERROR", "Database write failure.");
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  GET /v1/payments  —  Retrieve payments
    // ──────────────────────────────────────────────────────────────
    public async Task<PaymentListResponse> GetPaymentsAsync(
        IDictionary<string, string>? queryParams, string requestId, CancellationToken cancellationToken)
    {
        try
        {
            var page     = TryGetInt(queryParams, "page",     1,  min: 1);
            var pageSize = TryGetInt(queryParams, "pageSize", 20, min: 1);
            if (pageSize > 100)
                throw new PaymentServiceException(400, "INVALID_PAGE_SIZE", "Page size exceeds maximum.");

            // Build filter specs — stored as (whereClause, paramName, value) tuples.
            // Parameters are recreated fresh for each command to avoid the "parameter already
            // owned by another command" error that existed in the original code.
            var specs = new List<(string Clause, string Name, object Value)>();
            void AddStr(string key, string col)
            {
                if (queryParams?.TryGetValue(key, out var v) == true && !string.IsNullOrWhiteSpace(v))
                    specs.Add(($"p.{col} = @{key}", key, v));
            }
            void AddDate(string key, string op)
            {
                if (queryParams?.TryGetValue(key, out var v) == true && !string.IsNullOrWhiteSpace(v)
                    && DateTimeOffset.TryParse(v, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                    specs.Add(($"p.initiated_at {op} @{key}", key, (object)dt.UtcDateTime));
            }

            AddStr("userId",        "user_id");
            AddStr("planId",        "plan_id");
            AddStr("status",        "status");
            AddStr("paymentMethod", "payment_method");
            AddStr("currency",      "currency");
            AddDate("dateFrom", ">=");
            AddDate("dateTo",   "<=");

            var sqlWhere = specs.Count > 0
                ? " WHERE " + string.Join(" AND ", specs.Select(s => s.Clause))
                : string.Empty;

            // Helper — fresh NpgsqlParameter per command (cannot reuse across commands)
            NpgsqlParameter[] MakeParams() =>
                specs.Select(s => new NpgsqlParameter(s.Name, s.Value)).ToArray();

            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);

            // COUNT — disposed before the SELECT runs
            int total;
            await using (var countCmd = new NpgsqlCommand(
                $"SELECT COUNT(*) FROM payments p{sqlWhere}", conn))
            {
                countCmd.Parameters.AddRange(MakeParams());
                total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
            }

            // SELECT with LEFT JOIN to get invoiceId
            var payments = new List<PaymentItemResponse>();
            await using (var cmd = new NpgsqlCommand(
                $"SELECT p.payment_id, p.user_id, p.plan_id, p.amount, p.currency, " +
                $"p.payment_method, p.status, p.gateway_order_id, p.gateway_payment_id, " +
                $"p.initiated_at, p.completed_at, i.invoice_id " +
                $"FROM payments p LEFT JOIN invoices i ON i.payment_id = p.payment_id" +
                $"{sqlWhere} " +
                $"ORDER BY p.initiated_at DESC LIMIT @limit OFFSET @offset", conn))
            {
                cmd.Parameters.AddRange(MakeParams());
                cmd.Parameters.AddWithValue("limit",  pageSize);
                cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    payments.Add(new PaymentItemResponse
                    {
                        PaymentId        = reader.GetString(0),
                        UserId           = reader.GetString(1),
                        PlanId           = reader.GetString(2),
                        Amount           = reader.GetDecimal(3),
                        Currency         = reader.GetString(4),
                        PaymentMethod    = reader.GetString(5),
                        Status           = reader.GetString(6),
                        GatewayOrderId   = reader.GetString(7),
                        GatewayPaymentId = reader.IsDBNull(8)  ? null : reader.GetString(8),
                        InitiatedAt      = reader.GetDateTime(9),
                        CompletedAt      = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                        InvoiceId        = reader.IsDBNull(11) ? null : reader.GetString(11)
                    });
                }
            }

            return new PaymentListResponse
            {
                Status   = "success",
                Total    = total,
                Page     = page,
                PageSize = pageSize,
                Payments = payments
            };
        }
        catch (PaymentServiceException) { throw; }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[{requestId}] GetPaymentsAsync failure: {ex}");
            throw new PaymentServiceException(500, "DB_ERROR", "Database read failure.");
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Gateway — create Razorpay order with retry
    // ──────────────────────────────────────────────────────────────
    private async Task<string> CreateGatewayOrderAsync(
        decimal amount, string currency, string requestId, CancellationToken cancellationToken)
    {
        var keyId       = SecretsHelper.Get("gateway_key_id", "gateway_key_id");
        var keySecret   = SecretsHelper.Get("gateway_secret", "gateway_secret");
        var retryCount  = int.TryParse(Environment.GetEnvironmentVariable("GATEWAY_RETRY_COUNT"), out var rc) ? rc : 2;
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));

        // Razorpay expects amount in smallest currency unit (paise for INR)
        var amountUnits = (long)(amount * 100);
        var orderBody   = JsonSerializer.Serialize(new
        {
            amount   = amountUnits,
            currency = currency.ToUpperInvariant(),
            receipt  = $"rcpt_{Guid.NewGuid().ToString("N")[..12]}"
        });

        Exception? lastEx = null;
        for (var attempt = 0; attempt <= retryCount; attempt++)
        {
            if (attempt > 0) await Task.Delay(200, cancellationToken);
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders");
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                req.Content = new StringContent(orderBody, Encoding.UTF8, "application/json");
                using var resp = await _httpClient.SendAsync(req, cancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                    Console.Error.WriteLine($"[{requestId}] Gateway attempt {attempt + 1}: HTTP {(int)resp.StatusCode} {err}");
                    lastEx = new Exception($"Gateway HTTP {(int)resp.StatusCode}");
                    continue;
                }
                var json    = await resp.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var orderId = doc.RootElement.GetProperty("id").GetString();
                if (string.IsNullOrWhiteSpace(orderId)) throw new Exception("Gateway response missing order id.");
                return orderId;
            }
            catch (Exception ex) when (ex is not TaskCanceledException)
            {
                Console.Error.WriteLine($"[{requestId}] Gateway attempt {attempt + 1} exception: {ex.Message}");
                lastEx = ex;
            }
        }

        Console.Error.WriteLine($"[{requestId}] Gateway exhausted after {retryCount + 1} attempts: {lastEx}");
        throw new PaymentServiceException(500, "GATEWAY_ERROR", "Gateway order creation failed.");
    }

    // ──────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────
    private static async Task<long> NextSeqAsync(NpgsqlConnection conn, string seqName, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand($"SELECT nextval('{seqName}')", conn);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
    }

    private static int TryGetInt(IDictionary<string, string>? d, string key, int defaultVal, int min = int.MinValue)
    {
        if (d != null && d.TryGetValue(key, out var s) && int.TryParse(s, out var v) && v >= min) return v;
        return defaultVal;
    }

    private static T Deserialize<T>(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new PaymentServiceException(400, "MISSING_REQUIRED_FIELD", "Request body missing.");
        return JsonSerializer.Deserialize<T>(body)
               ?? throw new PaymentServiceException(400, "MISSING_REQUIRED_FIELD", "Malformed request body.");
    }

    private static void ValidateInitiate(InitiatePaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId)   || string.IsNullOrWhiteSpace(request.PlanId) ||
            !request.Amount.HasValue                     || string.IsNullOrWhiteSpace(request.Currency) ||
            string.IsNullOrWhiteSpace(request.PaymentMethod) || string.IsNullOrWhiteSpace(request.Email))
            throw new PaymentServiceException(400, "MISSING_REQUIRED_FIELD", "Missing required field.");

        if (!ValidPaymentMethods.Contains(request.PaymentMethod!))
            throw new PaymentServiceException(400, "INVALID_PAYMENT_METHOD",
                "Invalid payment method. Must be one of: UPI, CARD, NETBANKING, WALLET, BANK_TRANSFER.");
    }

    private static void ValidateVerify(VerifyPaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentId)          || string.IsNullOrWhiteSpace(request.GatewayPaymentId) ||
            string.IsNullOrWhiteSpace(request.GatewayOrderId)     || string.IsNullOrWhiteSpace(request.GatewaySignature) ||
            string.IsNullOrWhiteSpace(request.VerificationSource) || string.IsNullOrWhiteSpace(request.Status))
            throw new PaymentServiceException(400, "MISSING_REQUIRED_FIELD", "Missing required field.");

        if (!ValidVerificationSources.Contains(request.VerificationSource!))
            throw new PaymentServiceException(400, "INVALID_VERIFICATION_SOURCE",
                "Invalid verification source. Must be one of: WEBHOOK, POLLING, MANUAL.");
    }
}
