namespace PrimeNumberService.Security;

public interface ISecretProvider
{
    Task<DatabaseSecret> GetDatabaseSecretAsync(string secretName, CancellationToken cancellationToken = default);
}