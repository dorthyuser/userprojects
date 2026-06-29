using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace currency_calculator.Infrastructure;

public static class SecretHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private static readonly SecretClient? Client;
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly ILogger Logger = LoggerFactory.CreateLogger("SecretHelper");

    static SecretHelper()
    {
        try
        {
            var uri = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");
            if (!string.IsNullOrWhiteSpace(uri))
            {
                Client = new SecretClient(new Uri(uri), new DefaultAzureCredential());
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Secret client initialization failed.");
            Client = null;
        }
    }

    public static string Get(string secretName, string envFallback)
    {
        if (Cache.TryGetValue(secretName, out var cached)) return cached;
        try
        {
            if (Client is not null)
            {
                var value = Client.GetSecret(secretName).Value.Value ?? string.Empty;
                Cache[secretName] = value;
                return value;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Secret resolution failed.");
        }
        return Environment.GetEnvironmentVariable(envFallback) ?? string.Empty;
    }
}

public static class MySqlConnectionFactory
{
    public static MySqlConnector.MySqlConnection Create()
    {
        var host = SecretHelper.Get("MYSQL_HOST", "MYSQL_HOST");
        var port = SecretHelper.Get("MYSQL_PORT", "MYSQL_PORT");
        var user = SecretHelper.Get("MYSQL_USER", "MYSQL_USER");
        var password = SecretHelper.Get("MYSQL_PASSWORD", "MYSQL_PASSWORD");
        var database = SecretHelper.Get("MYSQL_DATABASE", "MYSQL_DATABASE");
        var cs = $"Server={host};Port={port};User ID={user};Password={password};Database={database};SslMode=Required;";
        return new MySqlConnector.MySqlConnection(cs);
    }
}