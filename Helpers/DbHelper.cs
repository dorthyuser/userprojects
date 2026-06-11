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

        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.MapEnum<CardholderType>("cardholder_type_enum");
        builder.MapEnum<TravelcardType>("travelcard_type_enum");
        builder.MapEnum<UserStatus>("user_status_enum");
        _dataSource = builder.Build();
    }

    public NpgsqlDataSource DataSource => _dataSource;
}
