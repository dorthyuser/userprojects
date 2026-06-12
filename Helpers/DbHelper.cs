using System;
using Npgsql;
using TravelCardFunctionApp.Models;

namespace TravelCardFunctionApp.Helpers;

public class DbHelper
{
    private readonly NpgsqlDataSource _dataSource;

    public DbHelper()
    {
        var connectionString = SecretHelper.Get("PostgresConnectionString", "POSTGRESQL_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=travelcards;Username=postgres;Password=postgres";
        }

        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.MapEnum<TravelcardType>("travelcard_type_enum");
        builder.MapEnum<CardholderType>("cardholder_type_enum");
        _dataSource = builder.Build();
    }

    public NpgsqlDataSource DataSource => _dataSource;
}
