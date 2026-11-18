using Microsoft.Extensions.Logging;

namespace BusTravelAccountsSaFunction;

public sealed class DatabaseCredentialProvider(DatabaseOptions options, ILogger<DatabaseCredentialProvider> logger)
{
    public DatabaseCredentials GetCredentials()
    {
        var usingKeyVaultSource = !string.IsNullOrWhiteSpace(options.KeyVault);

        if (string.IsNullOrWhiteSpace(options.Username) ||
            string.IsNullOrWhiteSpace(options.Password) ||
            string.IsNullOrWhiteSpace(options.Host) ||
            string.IsNullOrWhiteSpace(options.Dbname) ||
            string.IsNullOrWhiteSpace(options.Port))
        {
            if (usingKeyVaultSource)
            {
                logger.LogError("Database credentials source Key Vault was selected, but one or more resolved credential settings are missing or blank");
                throw new InvalidOperationException("Database Key Vault credential settings are not fully configured");
            }

            logger.LogError("Database fallback settings are not fully configured");
            throw new InvalidOperationException("Database fallback settings are not fully configured");
        }

        if (!int.TryParse(options.Port, out var port))
        {
            if (usingKeyVaultSource)
            {
                logger.LogError("Database credentials source Key Vault was selected, but the resolved port setting is invalid");
                throw new InvalidOperationException("Database Key Vault port setting is invalid");
            }

            logger.LogError("Database fallback port setting is invalid");
            throw new InvalidOperationException("Database fallback port setting is invalid");
        }

        logger.LogInformation("Database credentials source: {Source}", usingKeyVaultSource ? "Key Vault reference settings" : "environment settings");

        return new DatabaseCredentials(
            options.Username,
            options.Password,
            options.Host,
            options.Dbname,
            port);
    }
}
