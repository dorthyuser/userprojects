using System.Net.Http.Json;
using _123_sadsa_1232.Models;
using Microsoft.Extensions.Options;
using Npgsql;

namespace _123_sadsa_1232.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentService> _logger;
    private readonly PaymentApiOptions _options;
    private readonly string _connectionString;

    public PaymentService(IHttpClientFactory httpClientFactory, IOptions<PaymentApiOptions> options, ILogger<PaymentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
        _connectionString = BuildConnectionString();
    }

    public async Task<IEnumerable<PaymentDto>> GetAllAsync(string correlationId, CancellationToken cancellationToken)
    {
        var payments = new List<PaymentDto>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("select id, user_id, order_id, amount, currency, payment_method, provider, provider_transaction_id, status, created_at, updated_at, paid_at, description, metadata from payments order by id desc", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            payments.Add(MapPayment(reader));
        }
        return payments;
    }

    public async Task<PaymentDto?> GetByIdAsync(long id, string correlationId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("select id, user_id, order_id, amount, currency, payment_method, provider, provider_transaction_id, status, created_at, updated_at, paid_at, description, metadata from payments where id = @id", connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapPayment(reader) : null;
    }

    public async Task<PaymentDto> CreateAsync(PaymentUpsertRequest request, string correlationId, CancellationToken cancellationToken)
    {
        var payload = new IciciPaymentRequest(0, request.UserId, request.OrderId, request.Amount, request.Currency, request.PaymentMethod, request.Provider, request.ProviderTransactionId, request.Status, request.PaidAt, request.Description, request.Metadata, correlationId);
        var icici = await SendToIciciAsync(payload, correlationId, cancellationToken);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(@"insert into payments (user_id, order_id, amount, currency, payment_method, provider, provider_transaction_id, status, paid_at, description, metadata, created_at, updated_at) values (@user_id, @order_id, @amount, @currency, @payment_method, @provider, @provider_transaction_id, @status, @paid_at, @description, cast(@metadata as json), now(), now()) returning id", connection);
        AddRequestParameters(command, request, icici.TransactionId);
        var id = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        return new PaymentDto(id, request.UserId, request.OrderId, request.Amount, request.Currency, request.PaymentMethod, request.Provider, icici.TransactionId, request.Status, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, request.PaidAt, request.Description, request.Metadata);
    }

    public async Task<PaymentDto?> UpdateAsync(long id, PaymentUpsertRequest request, string correlationId, CancellationToken cancellationToken)
    {
        var existing = await GetByIdAsync(id, correlationId, cancellationToken);
        if (existing is null) return null;
        var payload = new IciciPaymentRequest(id, request.UserId, request.OrderId, request.Amount, request.Currency, request.PaymentMethod, request.Provider, request.ProviderTransactionId, request.Status, request.PaidAt, request.Description, request.Metadata, correlationId);
        var icici = await SendToIciciAsync(payload, correlationId, cancellationToken);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(@"update payments set user_id=@user_id, order_id=@order_id, amount=@amount, currency=@currency, payment_method=@payment_method, provider=@provider, provider_transaction_id=@provider_transaction_id, status=@status, paid_at=@paid_at, description=@description, metadata=cast(@metadata as json), updated_at=now() where id=@id", connection);
        command.Parameters.AddWithValue("id", id);
        AddRequestParameters(command, request, icici.TransactionId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return new PaymentDto(id, request.UserId, request.OrderId, request.Amount, request.Currency, request.PaymentMethod, request.Provider, icici.TransactionId, request.Status, existing.CreatedAt, DateTimeOffset.UtcNow, request.PaidAt, request.Description, request.Metadata);
    }

    private async Task<IciciPaymentResponse> SendToIciciAsync(IciciPaymentRequest payload, string correlationId, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("icici");
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.IciciEndpointPath)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation("correlation-id", correlationId);
        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"ICICI endpoint call failed with status {(int)response.StatusCode}: {body}");
        }
        return await response.Content.ReadFromJsonAsync<IciciPaymentResponse>(cancellationToken: cancellationToken) ?? new IciciPaymentResponse(Guid.NewGuid().ToString("N"), "accepted", correlationId, null);
    }

    private static void AddRequestParameters(NpgsqlCommand command, PaymentUpsertRequest request, string providerTransactionId)
    {
        command.Parameters.AddWithValue("user_id", request.UserId);
        command.Parameters.AddWithValue("order_id", (object?)request.OrderId ?? DBNull.Value);
        command.Parameters.AddWithValue("amount", request.Amount);
        command.Parameters.AddWithValue("currency", request.Currency);
        command.Parameters.AddWithValue("payment_method", request.PaymentMethod);
        command.Parameters.AddWithValue("provider", (object?)request.Provider ?? DBNull.Value);
        command.Parameters.AddWithValue("provider_transaction_id", (object?)providerTransactionId ?? DBNull.Value);
        command.Parameters.AddWithValue("status", request.Status);
        command.Parameters.AddWithValue("paid_at", (object?)request.PaidAt ?? DBNull.Value);
        command.Parameters.AddWithValue("description", (object?)request.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("metadata", request.Metadata ?? "{}");
    }

    private static PaymentDto MapPayment(NpgsqlDataReader reader)
    {
        string? metadata = reader["metadata"] is DBNull ? null : reader["metadata"]?.ToString();
        return new PaymentDto(
            (long)reader["id"],
            (long)reader["user_id"],
            reader["order_id"] is DBNull ? null : (long?)reader["order_id"],
            (decimal)reader["amount"],
            (string)reader["currency"],
            (string)reader["payment_method"],
            reader["provider"] is DBNull ? null : (string?)reader["provider"],
            reader["provider_transaction_id"] is DBNull ? null : (string?)reader["provider_transaction_id"],
            (string)reader["status"],
            (DateTimeOffset)reader["created_at"],
            (DateTimeOffset)reader["updated_at"],
            reader["paid_at"] is DBNull ? null : (DateTimeOffset?)reader["paid_at"],
            reader["description"] is DBNull ? null : (string?)reader["description"],
            metadata);
    }

    private string BuildConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("POSTGRESQL_HOST") ?? "";
        var port = Environment.GetEnvironmentVariable("POSTGRESQL_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("POSTGRESQL_DATABASE") ?? "";
        var username = Environment.GetEnvironmentVariable("POSTGRESQL_USERNAME") ?? "";
        var password = Environment.GetEnvironmentVariable("POSTGRESQL_PASSWORD") ?? "";
        var connectTimeout = Environment.GetEnvironmentVariable("POSTGRESQL_CONNECT_TIMEOUT") ?? "15";
        return $"Host={host};Port={port};Database={database};Username={username};Password={password};Timeout={connectTimeout};CommandTimeout={connectTimeout};Include Error Detail=true";
    }
}