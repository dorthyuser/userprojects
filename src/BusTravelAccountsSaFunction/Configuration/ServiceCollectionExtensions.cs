using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BusTravelAccountsSaFunction;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value);
        services.AddScoped<DatabaseOptions>(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value);
        services.AddScoped<DatabaseCredentialProvider>();

        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseCredentialProvider>>();
            var credentialProvider = new DatabaseCredentialProvider(options, logger);
            var credentials = credentialProvider.GetCredentials();

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = credentials.Host,
                Port = credentials.Port,
                Database = credentials.Dbname,
                Username = credentials.Username,
                Password = credentials.Password,
                MaxPoolSize = 16,
                Timeout = Math.Max(1, options.ResponseTimeoutMilliseconds / 1000),
                CommandTimeout = Math.Max(1, options.ResponseTimeoutMilliseconds / 1000),
                SslMode = SslMode.Require,
                TrustServerCertificate = false
            };

            return new NpgsqlDataSourceBuilder(builder.ConnectionString).Build();
        });

        services.AddScoped<IAccountsRepository, AccountsRepository>();
        services.AddScoped<IAccountsService, AccountsService>();

        return services;
    }
}
