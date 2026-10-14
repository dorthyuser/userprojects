using System.Text.Json;
using Amazon.Lambda.Core;
using Npgsql;
using combined_project.Models;

namespace combined_project.Services;

public sealed class PrimeService
{
    private static readonly int[] Primes = [101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199];
    private readonly NpgsqlDataSource _dataSource;

    public PrimeService(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<int> GetPrimeNumberBetween100And200Async(ILambdaContext? context = null)
    {
        context?.Logger.LogLine(JsonSerializer.Serialize(new { event = "service_start", operation = "SELECT", resource = "prime" }));
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT prime_number FROM prime_numbers WHERE prime_number BETWEEN @min AND @max ORDER BY prime_number ASC LIMIT 1", connection);
        cmd.Parameters.AddWithValue("min", 100);
        cmd.Parameters.AddWithValue("max", 200);
        var result = await cmd.ExecuteScalarAsync();
        if (result is null || result is DBNull)
        {
            return Primes[0];
        }
        var value = Convert.ToInt32(result);
        if (value < 100 || value > 200 || Array.IndexOf(Primes, value) < 0)
        {
            return Primes[0];
        }
        return value;
    }
}
