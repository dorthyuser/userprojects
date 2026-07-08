using System;
using System.Threading.Tasks;
using AdverseEventReporter.Models;
using Npgsql;

namespace AdverseEventReporter.Helpers;

public class DbHelper
{
    private readonly NpgsqlDataSource _dataSource;

    public DbHelper()
    {
        var connectionString = SecretHelper.Get("PostgresConnectionString", "POSTGRESQLCONNECTIONSTRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=adverse_events;Username=postgres;Password=postgres";
        }
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.MapEnum<Outcome>("outcome");
        builder.MapEnum<ActionTaken>("action_taken");
        builder.MapEnum<Priority>("priority");
        _dataSource = builder.Build();
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = await _dataSource.OpenConnectionAsync();
        return connection;
    }
}
