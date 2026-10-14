using Npgsql;
using combined_project.Services;

namespace combined_project.Infrastructure;

public static class DatabaseFactory
{
    public static NpgsqlDataSource CreateDataSource()
    {
        var cs = BuildConnectionString();
        var builder = new NpgsqlDataSourceBuilder(cs);
        return builder.Build();
    }

    private static string BuildConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = SecretsHelper.Get("host", "POSTGRESQLHOST"),
            Port = int.TryParse(SecretsHelper.Get("port", "POSTGRESQLPORT"), out var port) ? port : 5432,
            Database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE"),
            Username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            Password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD"),
            Timeout = 5,
            CommandTimeout = 30,
            Pooling = true,
            SearchPath = "public"
        };
        return builder.ConnectionString;
    }
}
