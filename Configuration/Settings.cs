using System;
using System.Collections.Generic;
using System.Globalization;

namespace Azfcsharp8Function;

public sealed class Settings
{
    public string BasePath { get; }
    public string PostgresUsername { get; }
    public string PostgresPassword { get; }
    public string PostgresHost { get; }
    public int PostgresPort { get; }
    public string PostgresDatabase { get; }

    public Settings()
    {
        BasePath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH") ?? string.Empty;
        var useVault = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI"));

        if (useVault)
        {
            PostgresUsername = Environment.GetEnvironmentVariable("POSTGRESQL_USERNAME") ?? string.Empty;
            PostgresPassword = Environment.GetEnvironmentVariable("POSTGRESQL_PASSWORD") ?? string.Empty;
            PostgresHost = Environment.GetEnvironmentVariable("POSTGRESQL_HOST") ?? string.Empty;
            PostgresPort = ParseInt(Environment.GetEnvironmentVariable("POSTGRESQL_PORT"));
            PostgresDatabase = Environment.GetEnvironmentVariable("POSTGRESQL_DATABASE") ?? string.Empty;
        }
        else
        {
            PostgresHost = Environment.GetEnvironmentVariable("DB_HOST") ?? string.Empty;
            PostgresPort = ParseInt(Environment.GetEnvironmentVariable("DB_PORT"));
            PostgresDatabase = Environment.GetEnvironmentVariable("DB_NAME") ?? string.Empty;
            PostgresUsername = Environment.GetEnvironmentVariable("DB_USERNAME") ?? string.Empty;
            PostgresPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;
        }
    }

    private static int ParseInt(string? value) => int.Parse(value ?? "0", CultureInfo.InvariantCulture);
}
