using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace RandomFunctionApp.Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<DbHelper> _logger;

        public DbHelper(IConfiguration config, ILogger<DbHelper> logger)
        {
            _logger = logger;
            var connectionString = config["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=postgres;Database=postgres;Pooling=true";
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            _dataSource = builder.Build();
            try
            {
                EnsureSchemaAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring database schema");
                throw;
            }
        }

        private async Task EnsureSchemaAsync()
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS random_values (
  id SERIAL PRIMARY KEY,
  value INTEGER NOT NULL,
  created_at TIMESTAMPTZ NOT NULL
);";
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InsertRandomValueAsync(int value)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO random_values(value, created_at) VALUES(@value, @created_at);";
                cmd.Parameters.AddWithValue("value", value);
                cmd.Parameters.AddWithValue("created_at", DateTime.UtcNow);
                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Inserted random value {Value} into database", value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting random value");
                throw;
            }
        }
    }
}
