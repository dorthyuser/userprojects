using Npgsql;
using Npgsql.NameTranslation;
using Dashboard2Lambda.Models;

namespace Dashboard2Lambda.Services;

public sealed class Service
{
    private static NpgsqlDataSource? _dataSource;
    private static readonly object Sync = new();

    public async Task<Response> CreateAsync(Request request)
    {
        await using var connection = await GetDataSource().OpenConnectionAsync();
        const string tableName = "dashboard2_records";
        using var cmd = new NpgsqlCommand($"INSERT INTO {tableName} (client_id, name, status) VALUES (@client_id, @name, @status) RETURNING id;", connection);
        cmd.Parameters.Add(new NpgsqlParameter("client_id", request.ClientId));
        cmd.Parameters.Add(new NpgsqlParameter("name", request.Name));
        cmd.Parameters.Add(new NpgsqlParameter("status", request.Status));
        var id = (await cmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
        return Response.Ok(id);
    }

    private static NpgsqlDataSource GetDataSource()
    {
        if (_dataSource is not null) return _dataSource;
        lock (Sync)
        {
            if (_dataSource is not null) return _dataSource;
            var builder = new NpgsqlDataSourceBuilder(BuildConnectionString());
            builder.MapEnum<RecordStatus>("record_status", new NpgsqlNullNameTranslator());
            _dataSource = builder.Build();
            return _dataSource;
        }
    }

    private static string BuildConnectionString()
    {
        var host = SecretsHelper.Get("host", "POSTGRESQLHOST");
        var port = SecretsHelper.Get("port", "POSTGRESQLPORT");
        var dbname = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE");
        var username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");
        return $"Host={host};Port={port};Database={dbname};Username={username};Password={password};Pooling=true;";
    }
}
