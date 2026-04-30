using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Models;
using Npgsql;

namespace Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;

        public DbHelper(IConfiguration configuration)
        {
            var connectionString = SecretHelper.Get("PostgresConnectionString", "POSTGRESQL_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var host = SecretHelper.Get("postgresql-host", "POSTGRESQL_HOST");
                var port = SecretHelper.Get("postgresql-port", "POSTGRESQL_PORT");
                var database = SecretHelper.Get("postgresql-database", "POSTGRESQL_DATABASE");
                var username = SecretHelper.Get("postgresql-username", "POSTGRESQL_USERNAME");
                var password = SecretHelper.Get("postgresql-password", "POSTGRESQL_PASSWORD");
                connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;Maximum Pool Size=100;";
            }

            var builder = new NpgsqlDataSourceBuilder(connectionString);
            _dataSource = builder.Build();
        }

        public async Task<IReadOnlyList<OrderResponse>> GetOrdersAsync()
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            const string sql = @"
SELECT o.id, o.account_id, o.order_data, o.created_at,
       a.id AS account_id, a.name, a.email, a.address
FROM orders o
INNER JOIN accounts a ON a.id = o.account_id
ORDER BY o.id DESC;";

            var result = new List<OrderResponse>();
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var orderData = reader.GetFieldValue<JsonElement>(2);
                result.Add(new OrderResponse
                {
                    id = reader.GetInt32(0),
                    account_id = reader.GetInt32(1),
                    order_data = orderData,
                    created_at = reader.GetDateTime(3),
                    account = new AccountInfo
                    {
                        id = reader.GetInt32(4),
                        name = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        email = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        address = reader.IsDBNull(7) ? null : reader.GetString(7)
                    }
                });
            }

            return result;
        }

        public async Task<object> CreateOrderAsync(CreateOrderRequest request)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                const string sql = @"
INSERT INTO orders (account_id, order_data)
VALUES (@account_id, @order_data::jsonb)
RETURNING id, created_at;";
                await using var cmd = new NpgsqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("account_id", request.account_id);
                cmd.Parameters.AddWithValue("order_data", request.order_data.GetRawText());

                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    throw new InvalidOperationException("Failed to insert order");
                }

                var id = reader.GetInt32(0);
                var createdAt = reader.GetDateTime(1);
                await tx.CommitAsync();
                return new { id, account_id = request.account_id, order_data = request.order_data, created_at = createdAt };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                const string sql = @"DELETE FROM orders WHERE id = @id;";
                await using var cmd = new NpgsqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("id", id);
                var affected = await cmd.ExecuteNonQueryAsync();
                await tx.CommitAsync();
                return affected > 0;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}