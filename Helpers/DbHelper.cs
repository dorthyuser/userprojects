using System;
using Npgsql;
using create_travelcard_prod.Models;

namespace create_travelcard_prod.Helpers;

public class DbHelper
{
    private readonly NpgsqlDataSource _dataSource;

    public DbHelper()
    {
        var connectionString = SecretHelper.Get("PostgresConnectionString", "PostgresConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=travelcards;Username=postgres;Password=postgres";
        }

        try
        {
            // Normalize and adjust settings for common local/dev scenarios to avoid authentication/SSL issues.
            var csb = new NpgsqlConnectionStringBuilder(connectionString);

            // If connecting to localhost, disable SSL to avoid TLS/auth negotiation issues with local Postgres instances.
            if (!string.IsNullOrEmpty(csb.Host) && (csb.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || csb.Host.Equals("127.0.0.1")))
            {
                csb.SslMode = SslMode.Disable;
                csb.TrustServerCertificate = true;
            }

            // Ensure required credentials are present for connection attempts. If missing, leave as-is but log.
            if (string.IsNullOrEmpty(csb.Username))
            {
                Console.WriteLine("[DbHelper] Warning: Username not provided in Postgres connection string.");
            }
            if (string.IsNullOrEmpty(csb.Password))
            {
                Console.WriteLine("[DbHelper] Warning: Password not provided in Postgres connection string.");
            }

            var builder = new NpgsqlDataSourceBuilder(csb.ConnectionString);
            builder.MapEnum<CardholderType>("cardholder_type_enum");
            builder.MapEnum<TravelcardType>("travelcard_type_enum");
            builder.MapEnum<UserStatus>("user_status_enum");
            _dataSource = builder.Build();
        }
        catch (Exception ex)
        {
            // Surface helpful diagnostics when datasource construction/authentication fails.
            Console.WriteLine($"[DbHelper] Failed building NpgsqlDataSource: {ex}");
            throw;
        }
    }

    public NpgsqlDataSource DataSource => _dataSource;
}
