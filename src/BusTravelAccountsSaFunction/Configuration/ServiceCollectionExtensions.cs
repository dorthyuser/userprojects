using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BusTravelAccountsSaFunction;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.Section))
            .ValidateDataAnnotations()
            .Validate(static options =>
                !string.IsNullOrWhiteSpace(options.KeyVault) ||
                (!string.IsNullOrWhiteSpace(options.Username) &&
                 !string.IsNullOrWhiteSpace(options.Password) &&
                 !string.IsNullOrWhiteSpace(options.Host) &&
                 !string.IsNullOrWhiteSpace(options.Dbname) &&
                 options.Port.HasValue),
                "Either Database__KeyVault must be configured or all fallback Database credential settings must be configured.")
            .Validate(static options =>
                string.IsNullOrWhiteSpace(options.KeyVault) ||
                !LooksLikeUnresolvedKeyVaultReference(options.KeyVault),
                "Database__KeyVault is configured but unresolved or invalid.")
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseConfiguration");

            if (!string.IsNullOrWhiteSpace(options.KeyVault))
            {
                logger.LogInformation("Database credential source selected: key-vault");
                logger.LogError("Configured key-vault source failed to provide database credentials");
                throw new InvalidOperationException("Configured key-vault source failed to provide database credentials.");
            }

            logger.LogInformation("Database credential source selected: environment fallback");

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = options.Host,
                Port = options.Port ?? 5432,
                Database = options.Dbname,
                Username = options.Username,
                Password = options.Password,
                MaxPoolSize = 16,
                Timeout = Math.Max(1, options.ResponseTimeoutMilliseconds / 1000),
                CommandTimeout = Math.Max(1, options.ResponseTimeoutMilliseconds / 1000),
                SslMode = SslMode.Require,
                TrustServerCertificate = false
            };

            return new NpgsqlDataSourceBuilder(builder.ConnectionString).Build();
        });

        services.AddScoped<IAccountsRepository, AccountsRepository>();
        services.AddScoped<IHealthRepository, HealthRepository>();
        services.AddScoped<IAccountsService, AccountsService>();
        services.AddScoped<IHealthService, HealthService>();

        return services;
    }

    private static bool LooksLikeUnresolvedKeyVaultReference(string value) =>
        value.TrimStart().StartsWith("@Microsoft.KeyVault(", StringComparison.OrdinalIgnoreCase);
}
