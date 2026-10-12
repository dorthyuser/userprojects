using PrimeNumberService.Data;
using PrimeNumberService.Models;
using PrimeNumberService.Validation;

namespace PrimeNumberService.Services;

public sealed class PrimeNumberService : IPrimeNumberService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly PrimeNumberValidator _validator;
    private readonly ILogger<PrimeNumberService> _logger;

    public PrimeNumberService(IDbConnectionFactory connectionFactory, PrimeNumberValidator validator, ILogger<PrimeNumberService> logger)
    {
        _connectionFactory = connectionFactory;
        _validator = validator;
        _logger = logger;
    }

    public async Task<int> GetPrimeNumberBetween100And200Async(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Service start: SELECT prime number in range 100..200");
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT prime_number FROM prime_numbers WHERE prime_number BETWEEN @min AND @max ORDER BY prime_number ASC LIMIT 1";
        var min = command.CreateParameter();
        min.ParameterName = "@min";
        min.Value = 100;
        command.Parameters.Add(min);
        var max = command.CreateParameter();
        max.ParameterName = "@max";
        max.Value = 200;
        command.Parameters.Add(max);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || result is DBNull)
        {
            throw new InvalidOperationException("No prime number found in range");
        }

        var primeNumber = Convert.ToInt32(result);
        _validator.Validate(primeNumber);
        _logger.LogInformation("Service completed successfully");
        return primeNumber;
    }
}