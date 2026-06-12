using System;
using System.Threading.Tasks;
using Npgsql;

namespace azurefunctionaeproject.Helpers;

public sealed class DbHelper
{
    private readonly NpgsqlDataSource _dataSource;

    public DbHelper()
    {
        var connectionString = SecretHelper.Get("PostgresConnectionString", "POSTGRESQL_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";
        }

        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.MapEnum<azurefunctionaeproject.Models.OutcomeEnum>("outcome_enum");
        builder.MapEnum<azurefunctionaeproject.Models.ActionTakenEnum>("action_taken_enum");
        builder.MapEnum<azurefunctionaeproject.Models.PriorityEnum>("priority_enum");
        _dataSource = builder.Build();
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = await _dataSource.OpenConnectionAsync();
        return connection;
    }
}
