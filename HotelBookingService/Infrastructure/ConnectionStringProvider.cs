using Microsoft.Extensions.Configuration;

namespace HotelBookingService.Infrastructure;

public static class ConnectionStringProvider
{
    public static string Build(IConfiguration configuration)
    {
        var host = SecretsHelper.Get("host", "POSTGRESQLHOST");
        var port = SecretsHelper.Get("port", "POSTGRESQLPORT");
        var dbName = SecretsHelper.Get("dbname", "POSTGRESQLDBNAME");
        var username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");

        var configured = configuration.GetConnectionString("Postgres");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return $"Host={host};Port={port};Database={dbName};Username={username};Password={password};Pooling=true;Maximum Pool Size=20;Timeout=15;Command Timeout=30";
    }
}
