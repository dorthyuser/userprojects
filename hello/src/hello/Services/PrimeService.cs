using hello.Infrastructure;
using Npgsql;

namespace hello.Services;

public sealed class PrimeService : IPrimeService
{
    private static readonly int[] Primes = { 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199 };
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PrimeService> _logger;

    public PrimeService(IDbConnectionFactory connectionFactory, ILogger<PrimeService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> GetPrimeNumberBetween100And200Async(CancellationToken cancellationToken)
    {
        _logger.LogInformation("service_start operation=SELECT resource=prime");
        try
        {
            await using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await using var cmd = new NpgsqlCommand("SELECT prime_number FROM prime_numbers WHERE prime_number BETWEEN @min AND @max ORDER BY prime_number ASC LIMIT 1", conn);
            cmd.Parameters.AddWithValue("min", 100);
            cmd.Parameters.AddWithValue("max", 200);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            if (result is null || result is DBNull)
            {
                _logger.LogWarning("No prime number found in range; using safe default.");
                return Primes[0];
            }

            var value = Convert.ToInt32(result);
            if (!Primes.Contains(value))
            {
                throw new InvalidOperationException("Database returned a non-prime value.");
            }

            _logger.LogInformation("db_operation table=prime_numbers operation=SELECT");
            return value;
        }
        catch (PostgresException)
        {
            throw;
        }
        catch (Exception)
        {
            throw;
        }
    }
}
