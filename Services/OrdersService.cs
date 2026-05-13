using System.Collections.Concurrent;
using Npgsql;
using Npgsql.NameTranslation;
using test_capi_1111123323333.Models;

namespace test_capi_1111123323333.Services;

public sealed class OrdersService : IOrdersService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<OrdersService> _logger;

    public OrdersService(ILogger<OrdersService> logger)
    {
        _logger = logger;
        var host = SecretHelper.Get("POSTGRESQLHOST", "POSTGRESQLHOST");
        var port = SecretHelper.Get("POSTGRESQLPORT", "POSTGRESQLPORT");
        var database = SecretHelper.Get("POSTGRESQLDATABASE", "POSTGRESQLDATABASE");
        var username = SecretHelper.Get("POSTGRESQLUSERNAME", "POSTGRESQLUSERNAME");
        var password = SecretHelper.Get("POSTGRESQLPASSWORD", "POSTGRESQLPASSWORD");
        var connStr = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        var builder = new NpgsqlDataSourceBuilder(connStr);
        _dataSource = builder.Build();
    }

    public async Task<IEnumerable<OrderResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DB operation: {Operation} into {Table}", "SELECT", "orders");
        try
        {
            var result = new List<OrderResponse>();
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT order_id, customer_name, product_name, quantity, order_date FROM orders ORDER BY order_date DESC";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new OrderResponse
                {
                    OrderId = reader.GetGuid(0),
                    CustomerName = reader.GetString(1),
                    ProductName = reader.GetString(2),
                    Quantity = reader.GetInt32(3),
                    OrderDate = reader.GetFieldValue<DateTimeOffset>(4)
                });
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB error: table={Table}, operation={Operation}", "orders", "SELECT");
            throw;
        }
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DB operation: {Operation} into {Table}", "SELECT", "orders");
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT order_id, customer_name, product_name, quantity, order_date FROM orders WHERE order_id = @order_id";
            cmd.Parameters.AddWithValue("order_id", orderId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            return new OrderResponse
            {
                OrderId = reader.GetGuid(0),
                CustomerName = reader.GetString(1),
                ProductName = reader.GetString(2),
                Quantity = reader.GetInt32(3),
                OrderDate = reader.GetFieldValue<DateTimeOffset>(4)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB error: table={Table}, operation={Operation}", "orders", "SELECT");
            throw;
        }
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating request...");
        if (request.OrderDate > DateTimeOffset.UtcNow.AddYears(1))
        {
            _logger.LogWarning("Validation failed: field={Field}, reason={Reason}", "orderDate", "must not be too far in the future");
            throw new ArgumentException("orderDate must not be too far in the future");
        }
        _logger.LogInformation("Validation passed.");
        _logger.LogInformation("DB operation: {Operation} into {Table}", "INSERT", "orders");
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO orders (customer_name, product_name, quantity, order_date) VALUES (@customer_name, @product_name, @quantity, @order_date) RETURNING order_id";
            cmd.Parameters.AddWithValue("customer_name", request.CustomerName);
            cmd.Parameters.AddWithValue("product_name", request.ProductName);
            cmd.Parameters.AddWithValue("quantity", request.Quantity);
            cmd.Parameters.AddWithValue("order_date", request.OrderDate);
            var orderId = (Guid)(await cmd.ExecuteScalarAsync(cancellationToken))!;
            await tx.CommitAsync(cancellationToken);
            _logger.LogInformation("DB success: table={Table}, id={Id}", "orders", orderId);
            _logger.LogInformation("Request completed. id={Id}", orderId);
            return new OrderResponse { OrderId = orderId, CustomerName = request.CustomerName, ProductName = request.ProductName, Quantity = request.Quantity, OrderDate = request.OrderDate };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "DB error: table={Table}, operation={Operation}", "orders", "INSERT");
            throw;
        }
    }

    public async Task<OrderResponse?> UpdateAsync(Guid orderId, UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating request...");
        if (request.OrderDate > DateTimeOffset.UtcNow.AddYears(1))
        {
            _logger.LogWarning("Validation failed: field={Field}, reason={Reason}", "orderDate", "must not be too far in the future");
            throw new ArgumentException("orderDate must not be too far in the future");
        }
        _logger.LogInformation("Validation passed.");
        _logger.LogInformation("DB operation: {Operation} into {Table}", "UPDATE", "orders");
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE orders SET customer_name=@customer_name, product_name=@product_name, quantity=@quantity, order_date=@order_date WHERE order_id=@order_id";
            cmd.Parameters.AddWithValue("order_id", orderId);
            cmd.Parameters.AddWithValue("customer_name", request.CustomerName);
            cmd.Parameters.AddWithValue("product_name", request.ProductName);
            cmd.Parameters.AddWithValue("quantity", request.Quantity);
            cmd.Parameters.AddWithValue("order_date", request.OrderDate);
            var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            if (rows == 0)
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }
            await tx.CommitAsync(cancellationToken);
            _logger.LogInformation("DB success: table={Table}, id={Id}", "orders", orderId);
            _logger.LogInformation("Request completed. id={Id}", orderId);
            return new OrderResponse { OrderId = orderId, CustomerName = request.CustomerName, ProductName = request.ProductName, Quantity = request.Quantity, OrderDate = request.OrderDate };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "DB error: table={Table}, operation={Operation}", "orders", "UPDATE");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DB operation: {Operation} into {Table}", "DELETE", "orders");
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM orders WHERE order_id = @order_id";
            cmd.Parameters.AddWithValue("order_id", orderId);
            var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            if (rows > 0)
            {
                _logger.LogInformation("DB success: table={Table}, id={Id}", "orders", orderId);
                _logger.LogInformation("Request completed. id={Id}", orderId);
            }
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB error: table={Table}, operation={Operation}", "orders", "DELETE");
            throw;
        }
    }
}