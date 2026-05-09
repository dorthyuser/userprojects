using System;
using System.Threading;
using System.Threading.Tasks;
using jkjkjkjkjkjkjkjkjkjkjkjkjkjjk.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace jkjkjkjkjkjkjkjkjkjkjkjkjkjjk.Services
{
    public sealed class OrderService : IOrderService
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ILogger<OrderService> logger)
        {
            _logger = logger;
            var host = SecretHelper.Get("POSTGRESQL_HOST", "POSTGRESQL_HOST");
            var port = SecretHelper.Get("POSTGRESQL_PORT", "POSTGRESQL_PORT");
            var database = SecretHelper.Get("POSTGRESQL_DATABASE", "POSTGRESQL_DATABASE");
            var username = SecretHelper.Get("POSTGRESQL_USERNAME", "POSTGRESQL_USERNAME");
            var password = SecretHelper.Get("POSTGRESQL_PASSWORD", "POSTGRESQL_PASSWORD");
            var connStr = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
            var builder = new NpgsqlDataSourceBuilder(connStr);
            _dataSource = builder.Build();
        }

        public async Task<OrderResponse> GetByIdAsync(string orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating request...");
            if (string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogWarning("Validation failed: field={Field}, reason={Reason}", "orderId", "required");
                throw new ArgumentException("orderId is required");
            }
            _logger.LogInformation("Validation passed.");

            _logger.LogInformation("DB operation: {Operation} into {Table}", "SELECT", "orders");
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT order_id, customer_name, product_code, quantity, unit_price, status FROM orders WHERE order_id = @orderId";
            cmd.Parameters.AddWithValue("orderId", orderId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new KeyNotFoundException();
            }
            var response = new OrderResponse
            {
                OrderId = reader.GetString(0),
                CustomerName = reader.GetString(1),
                ProductCode = reader.GetString(2),
                Quantity = reader.GetInt32(3),
                UnitPrice = reader.GetDecimal(4),
                Status = Enum.Parse<OrderStatusEnum>(reader.GetString(5), true)
            };
            _logger.LogInformation("Request completed. id={Id}", response.OrderId);
            return response;
        }

        public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating request...");
            if (string.IsNullOrWhiteSpace(request.CustomerName)) throw new ArgumentException("customerName is required");
            if (string.IsNullOrWhiteSpace(request.ProductCode)) throw new ArgumentException("productCode is required");
            if (request.Quantity <= 0) throw new ArgumentException("quantity must be greater than 0");
            if (request.UnitPrice <= 0) throw new ArgumentException("unitPrice must be greater than 0");
            _logger.LogInformation("Validation passed.");

            var orderId = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            try
            {
                _logger.LogInformation("DB operation: {Operation} into {Table}", "INSERT", "orders");
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO orders (order_id, customer_name, product_code, quantity, unit_price, status) VALUES (@orderId, @customerName, @productCode, @quantity, @unitPrice, @status)";
                cmd.Parameters.AddWithValue("orderId", orderId);
                cmd.Parameters.AddWithValue("customerName", request.CustomerName);
                cmd.Parameters.AddWithValue("productCode", request.ProductCode);
                cmd.Parameters.AddWithValue("quantity", request.Quantity);
                cmd.Parameters.AddWithValue("unitPrice", request.UnitPrice);
                cmd.Parameters.AddWithValue("status", OrderStatusEnum.Pending.ToString());
                await cmd.ExecuteNonQueryAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                _logger.LogInformation("DB success: table={Table}, id={Id}", "orders", orderId);
                _logger.LogInformation("Request completed. id={Id}", orderId);
                return new OrderResponse { OrderId = orderId, CustomerName = request.CustomerName, ProductCode = request.ProductCode, Quantity = request.Quantity, UnitPrice = request.UnitPrice, Status = OrderStatusEnum.Pending };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "DB error: table={Table}, operation={Operation}", "orders", "INSERT");
                throw;
            }
        }

        public async Task<OrderResponse> UpdateAsync(string orderId, UpdateOrderRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating request...");
            if (string.IsNullOrWhiteSpace(orderId)) throw new ArgumentException("orderId is required");
            if (string.IsNullOrWhiteSpace(request.CustomerName)) throw new ArgumentException("customerName is required");
            if (string.IsNullOrWhiteSpace(request.ProductCode)) throw new ArgumentException("productCode is required");
            if (request.Quantity <= 0) throw new ArgumentException("quantity must be greater than 0");
            if (request.UnitPrice <= 0) throw new ArgumentException("unitPrice must be greater than 0");
            _logger.LogInformation("Validation passed.");

            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            try
            {
                _logger.LogInformation("DB operation: {Operation} into {Table}", "UPDATE", "orders");
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "UPDATE orders SET customer_name = @customerName, product_code = @productCode, quantity = @quantity, unit_price = @unitPrice WHERE order_id = @orderId";
                cmd.Parameters.AddWithValue("orderId", orderId);
                cmd.Parameters.AddWithValue("customerName", request.CustomerName);
                cmd.Parameters.AddWithValue("productCode", request.ProductCode);
                cmd.Parameters.AddWithValue("quantity", request.Quantity);
                cmd.Parameters.AddWithValue("unitPrice", request.UnitPrice);
                var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                if (rows == 0) throw new KeyNotFoundException();
                await tx.CommitAsync(cancellationToken);
                _logger.LogInformation("DB success: table={Table}, id={Id}", "orders", orderId);
                _logger.LogInformation("Request completed. id={Id}", orderId);
                return new OrderResponse { OrderId = orderId, CustomerName = request.CustomerName, ProductCode = request.ProductCode, Quantity = request.Quantity, UnitPrice = request.UnitPrice, Status = OrderStatusEnum.Confirmed };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "DB error: table={Table}, operation={Operation}", "orders", "UPDATE");
                throw;
            }
        }

        public async Task<CancelOrderResponse> CancelAsync(string orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating request...");
            if (string.IsNullOrWhiteSpace(orderId)) throw new ArgumentException("orderId is required");
            _logger.LogInformation("Validation passed.");

            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            try
            {
                _logger.LogInformation("DB operation: {Operation} into {Table}", "UPDATE", "orders");
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "UPDATE orders SET status = @status WHERE order_id = @orderId";
                cmd.Parameters.AddWithValue("orderId", orderId);
                cmd.Parameters.AddWithValue("status", OrderStatusEnum.Cancelled.ToString());
                var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                if (rows == 0) throw new KeyNotFoundException();
                await tx.CommitAsync(cancellationToken);
                _logger.LogInformation("DB success: table={Table}, id={Id}", "orders", orderId);
                _logger.LogInformation("Request completed. id={Id}", orderId);
                return new CancelOrderResponse { OrderId = orderId, Cancelled = true };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "DB error: table={Table}, operation={Operation}", "orders", "UPDATE");
                throw;
            }
        }
    }
}