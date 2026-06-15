using System;
using Npgsql;
using travelcard_functions.Models;

namespace travelcard_functions.Helpers;

public sealed class DbHelper
{
    private readonly NpgsqlDataSource _dataSource;

    public DbHelper()
    {
        var connectionString = SecretHelper.Get("PostgresConnectionString", "POSTGRESQLCONNECTIONSTRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=travelcards;Username=postgres;Password=postgres";
        }

        // Append connection pool settings — industry standard
        // Min=2 ensures connections are kept warm, Max=20 limits resource usage
        // Idle Lifetime=300s releases idle connections after 5 minutes
        // Command Timeout=30s prevents long-running queries from hanging
        connectionString +=
            ";Minimum Pool Size=2" +
            ";Maximum Pool Size=20" +
            ";Connection Idle Lifetime=300" +
            ";Connection Pruning Interval=10" +
            ";Command Timeout=30";

        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.MapEnum<TravelcardType>("travelcard_type_enum");
        builder.MapEnum<CardholderType>("cardholder_type_enum");
        _dataSource = builder.Build();
    }

    public NpgsqlDataSource DataSource => _dataSource;
}
